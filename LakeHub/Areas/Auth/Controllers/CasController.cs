using LakeHub.Areas.Auth.Models;
using LakeHub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Refit;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace LakeHub.Areas.Auth.Controllers
{
    [Area("Auth")]
    public class CasController : Controller
    {
        private readonly ILogger<CasController> _logger;
        private readonly ICASRestProtocol _cas;

        public CasController(ILogger<CasController> logger, ICASRestProtocol cas)
        {
            _logger = logger;
            _cas = cas;
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
        public async Task<IActionResult> SignInAsync(string? ReturnUrl, CasSignInForm cred)
        {
            if (!ModelState.IsValid) return View();
            try
            {
                JsonDocument json = await _cas.Users(new CASUsernamePasswordCredential(cred.InputCASId, cred.InputPassword));
                var JsonPrincipleAttribs = json.RootElement.GetProperty("authentication").GetProperty("successes").GetProperty("RestAuthenticationHandler").GetProperty("principal").GetProperty("attributes");

                LoginSuccess = true;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, cred.InputCASId),
                    new Claim("cas:password", cred.InputPassword),
                    new Claim(ClaimTypes.Name, JsonPrincipleAttribs.GetProperty("name").GetString()!),
                    // new Claim("pycc", JsonPrincipleAttribs.GetProperty("pycc").GetString()!),
                    new Claim("cas:organization", JsonPrincipleAttribs.GetProperty("organization").GetString()!)
                };

                var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, "id", "role");
                claimsIdentity.AddClaims(claims);

                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = cred.TrustThisDevice,
                };
                _logger.LogInformation("User {CASId} signed in from {Address}", cred.InputCASId, HttpContext.Connection.RemoteIpAddress);
                return SignIn(new ClaimsPrincipal(claimsIdentity), authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
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

        [Authorize]
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
