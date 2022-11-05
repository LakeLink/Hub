using LakeHub.Areas.Auth.Models;
using LakeHub.Models;
using LakeHub.Options;
using LakeHub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;

namespace LakeHub.Areas.Auth.Controllers
{
    [Area("Auth")]
    public class CasController : Controller
    {
        private readonly ILogger<CasController> _logger;
        private readonly ICASRestProtocol _cas;
        private readonly LakeHubContext _db;
        private readonly MailOptions _mailOptions;

        public CasController(ILogger<CasController> logger, ICASRestProtocol cas, IOptionsMonitor<MailOptions> options, LakeHubContext dbCtx)
        {
            _logger = logger;
            _cas = cas;
            _db = dbCtx;
            _mailOptions = options.CurrentValue;
        }
        public IActionResult Index()
        {
            return View();
        }

        public bool LoginSuccess { get; set; }
        [HttpGet]
        public IActionResult SignIn()
        {
            return View(new CasSignInForm());
        }

        //https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/cookie
        [HttpPost]
        public async Task<IActionResult> SignIn(string? ReturnUrl, CasSignInForm cred)
        {
            if (!ModelState.IsValid) return View();
            try
            {
                JsonDocument json = await _cas.Users(new CASUsernamePasswordCredential(cred.InputCASId, cred.InputPassword));
                var JsonPrincipleAttribs = json.RootElement.GetProperty("authentication").GetProperty("successes").GetProperty("RestAuthenticationHandler").GetProperty("principal").GetProperty("attributes");

                LoginSuccess = true;

                var user = await _db.User.FindAsync(cred.InputCASId);
                if (user == null)
                {
                    user = new DbUser(cred.InputCASId!, JsonPrincipleAttribs.GetProperty("name").GetString()!)
                    {
                        Org = JsonPrincipleAttribs.GetProperty("organization").GetString()!,
                        // orAGnizEtion_id ???
                        OrgId = Convert.ToInt32(JsonPrincipleAttribs.GetProperty("oragnizetion_id").GetString()!),
                        Identity = JsonPrincipleAttribs.GetProperty("identity").GetString()!
                    };

                    _db.User.Add(user);
                }

                var task = _db.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, cred.InputCASId),
                    new Claim("cas:password", cred.InputPassword),
                    new Claim(ClaimTypes.Name, user.Name!),
                    // new Claim("pycc", JsonPrincipleAttribs.GetProperty("pycc").GetString()!),
                    new Claim("cas:organization", user.Org!)
                };

                var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, "id", "role");
                claimsIdentity.AddClaims(claims);

                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = cred.TrustThisDevice,
                };

                // If don't work, please check options.LoginPath at Services.AddCookie()
                if (user.Email == null || !user.EmailVerified)
                {
                    authProperties.RedirectUri = Url.Action("Email", new
                    {
                        ReturnUrl
                    });
                }
                else
                {
                    // https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs#L416
                    authProperties.RedirectUri = ReturnUrl ?? Url.Page("/Index");
                }

                var ret = SignIn(new ClaimsPrincipal(claimsIdentity), authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("User {CASId} signed in from {Address}", cred.InputCASId, HttpContext.Connection.RemoteIpAddress);
                await task;
                return ret;
            }
            catch (ApiException e)
            {
                if (e.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (e.Content != null && e.Content.Contains("AccountNotFoundException"))
                    {
                        ModelState.AddModelError("InputCASId", "Incorrect ID.");
                        _logger.LogWarning("{Address} tried to sign in a nonexistent account {CASId}.", HttpContext.Connection.RemoteIpAddress, cred.InputCASId);
                    }
                    else
                    {
                        ModelState.AddModelError("InputPassword", "Incorrect password.");
                        _logger.LogWarning("{Address} tried to sign in as {CASId} with wrong password.", HttpContext.Connection.RemoteIpAddress, cred.InputCASId);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Gateway returned an unexpected error.");
                }
                return View();
            }
            // catch (Exception e)
            // {
            //     _logger.LogError(e, "Unexpected error with user {UserID}", UserID);

            //     //ModelState.AddModelError(string.Empty, "An unexpected internal error has occured.");
            //     //return Page();
            // }
        }

        public async Task<IActionResult> SignOutAsync()
        {
            var signOut = HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
            await signOut;

            return RedirectToPage("/Index");
        }

        [HttpGet]
        [Authorize(CookieAuthenticationDefaults.AuthenticationScheme)]
        public IActionResult Email()
        {
            return View(new CasEmailForm());
        }

        [HttpPost]
        [Authorize(CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> EmailAsync(string? ReturnUrl, CasEmailForm e)
        {
            if (ModelState["InputEmail"]!.ValidationState == ModelValidationState.Invalid /* || !Email.ToLower().EndsWith("@westlake.edu.cn")*/)
            {
                ModelState.AddModelError(string.Empty, "Invalid e-mail address.");
                return View(e);
            } // Now we have a valid email input
            string casId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DbUser user = (await _db.User.FindAsync(casId))!;
            if (e.InputVerifyCode == null) // No verification code, generate it
            {
                int code = Random.Shared.Next(1, 999999);

                // Must wait: ensure the code is sent.
                await SendVerifyCode(user.Name, e.InputEmail, code);
                _logger.LogInformation("Verification email sent to {Email} for user {CASId} from {Address}", e.InputEmail, casId, HttpContext.Connection.RemoteIpAddress);

                HttpContext.Session.SetString("pendingEmail", e.InputEmail);
                HttpContext.Session.SetInt32("verifyCode", code);
                e.VerifyCodeSent = true;
                return View(e);
            }
            else // Verification code submitted
            {
                // Do not use InputEmail: can be modified by user (IDOR)
                if (HttpContext.Session.GetString("pendingEmail") != null && HttpContext.Session.GetInt32("verifyCode") == e.InputVerifyCode)
                {
                    user.Email = HttpContext.Session.GetString("pendingEmail");
                    user.EmailVerified = true;
                    user.IPAtEmailVerify = HttpContext.Connection.RemoteIpAddress!.ToString();
                    var task = _db.SaveChangesAsync();
                    _logger.LogInformation("Email verified for user {CASId} from {Address}", casId, HttpContext.Connection.RemoteIpAddress);
                    await task;

                    if (ReturnUrl == null) return RedirectToPage("/Index");
                    else return Redirect(ReturnUrl);
                }
                else // Something goes wrong, wipe everything.
                {
                    ModelState.AddModelError(string.Empty, "Invalid verification code.");
                    HttpContext.Session.Remove("verifyCode");
                    HttpContext.Session.Remove("pendingEmail");
                    e.VerifyCodeSent = false;
                    _logger.LogWarning("Invalid verification code for {CASId} and {Email} from {Address}", casId, e.InputEmail, HttpContext.Connection.RemoteIpAddress);
                    return View(e);
                }
            }
        }
        private async Task SendVerifyCode(string name, string email, int code)
        {
            // No need for MailKit
            //using var client = new SmtpClient();
            //await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable);
            //Task authTask = client.AuthenticateAsync(_options.SysEmail, _options.SmtpPassword);

            //var m = new MimeMessage();
            //m.From.Add(new MailboxAddress("Discourse", _options.SysEmail));
            //m.To.Add(new MailboxAddress(name, email));
            //m.Subject = "Email Verification - Discourse";
            //m.Body = new TextPart("plain")
            //{
            //    Text = 
            //};

            //await authTask;
            //await client.SendAsync(m);
            //await client.DisconnectAsync(true);

            using var client = new SmtpClient(_mailOptions.SmtpServer, _mailOptions.SmtpPort);

            client.EnableSsl = true; // Enable STARTTLS
            client.Credentials = new NetworkCredential(_mailOptions.SysEmail, _mailOptions.SmtpPassword);
            var msg = new MailMessage(
                new MailAddress(_mailOptions.SysEmail!), new MailAddress(email, name)
                )
            {
                Subject = "Email Verification - LakeHub",
                Body = $"Welcome to Discourse, {name}!\n\nYour verification code is {code}.\n\nHave a nice day!"
            };
            await client.SendMailAsync(msg);
        }


        private async Task<string> refreshTGT(CASUsernamePasswordCredential cred)
        {
            var resp = await _cas.Tickets(cred);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("TGT creation for {CASId} failed due to wrong credential.", cred.username);
                throw new UnauthorizedAccessException("Wrong CAS credential.");
            }
            await resp.EnsureSuccessStatusCodeAsync();

            string TGT = getTGTFromUrl(resp.Headers.Location)!;
            if (!TGT.StartsWith("TGT")) throw new Exception("Invalid TGT returned.");

            HttpContext.Session.SetString("TGT", TGT);

            return TGT;
        }

        private string? getTGTFromUrl(Uri? url)
        {
            return Path.GetFileName(url?.AbsolutePath);
        }

        public async Task<IActionResult> Connect(string serviceUrl)
        {
            await HttpContext.Session.LoadAsync();
            try
            {
                var cred = new CASUsernamePasswordCredential(User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue("cas:password"));
                string TGT = HttpContext.Session.GetString("TGT") ?? await refreshTGT(cred);


                // fetch ST
                var resp = await _cas.Tickets(TGT, new Dictionary<string, object> { { "service", serviceUrl } });
                await resp.EnsureSuccessStatusCodeAsync();

                string ST = resp.Content!;
                if (!ST.StartsWith("ST")) throw new Exception("Invalid ST returned.");
                _logger.LogInformation("User {CASId} obtained ST for {Service} from {Address}", cred.username, serviceUrl, HttpContext.Connection.RemoteIpAddress);
                return Redirect(QueryHelpers.AddQueryString(serviceUrl, "ticket", ST));
            }
            catch (UnauthorizedAccessException)
            {
                return Challenge();
            }
        }
    }

}
