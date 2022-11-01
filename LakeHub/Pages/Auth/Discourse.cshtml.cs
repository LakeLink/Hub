using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MimeKit;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using LakeHub.Models;
using LakeHub.Options;

namespace LakeHub.Pages.Auth;

[Authorize]
public class DiscourseModel : PageModel
{
    private readonly ILogger<DiscourseModel> _logger;
    private readonly DiscourseOptions _options;
    private readonly LakeHubContext _db;

    public bool VerifyCodeSent { get; set; } = false;

    [BindProperty]
    [DataType(DataType.EmailAddress)]
    public string InputEmail { get; set; } = string.Empty;

    [BindProperty]
    public int? InputVerifyCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? ChangeEmail { get; set; }

    public DiscourseModel(ILogger<DiscourseModel> logger, IOptionsMonitor<DiscourseOptions> options, LakeHubContext dbCtx)
    {
        _logger = logger;
        _options = options.CurrentValue;
        _db = dbCtx;
    }

    private string SsoSignIn(string sso, string sig, Dictionary<string, string?> identity)
    {
        using HMACSHA256 hmac = new(Encoding.ASCII.GetBytes(_options.ConnectSecret!));

        byte[] computedHash = hmac.ComputeHash(Encoding.ASCII.GetBytes(sso!));
        if (Convert.ToHexString(computedHash) != sig!.ToUpper())
        {
            throw new ArgumentException("Wrong signature from Discourse: " + sig);
        }

        string inPayload = Encoding.ASCII.GetString(Convert.FromBase64String(sso!));
        var dict = QueryHelpers.ParseQuery(inPayload);

        identity.Add("nonce", dict["nonce"]);

        string outPayload = QueryString.Create(identity).ToString()[1..]; // remove leading '?'
        outPayload = Convert.ToBase64String(Encoding.ASCII.GetBytes(outPayload));

        byte[] outHash = hmac.ComputeHash(Encoding.ASCII.GetBytes(outPayload));
        string outSig = Convert.ToHexString(outHash).ToLower();

        return QueryHelpers.AddQueryString(dict["return_sso_url"], new Dictionary<string, string?>()
            {
                { "sso", outPayload },
                { "sig", outSig }
            });
    }

    private async Task SendVerifyCode(string name, string email, int code)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable);
        Task authTask = client.AuthenticateAsync(_options.SysEmail, _options.SmtpPassword);

        var m = new MimeMessage();
        m.From.Add(new MailboxAddress("Discourse", _options.SysEmail));
        m.To.Add(new MailboxAddress(name, email));
        m.Subject = "Email Verification - Discourse";
        m.Body = new TextPart("plain")
        {
            Text = $"Welcome to Discourse, {name}!\n\nYour verification code is {code}.\n\nHave a nice day!"
        };

        await authTask;
        await client.SendAsync(m);
        await client.DisconnectAsync(true);
    }

    public async Task<IActionResult> OnPost(string sso, string sig)
    {
        if (ModelState["InputEmail"]!.ValidationState == ModelValidationState.Invalid /* || !Email.ToLower().EndsWith("@westlake.edu.cn")*/)
        {
            ModelState.AddModelError(string.Empty, "Invalid e-mail address.");
            return Page();
        } // Now we have a valid email input
        string casId = User.FindFirst("id")!.Value;
        DbUser user = (await _db.User.FindAsync(casId))!;
        if (InputVerifyCode == null) // No verification code, generate it
        {
            int code = Random.Shared.Next(1, 999999);

            // Must wait: ensure the code is sent.
            await SendVerifyCode(user.Name, InputEmail, code);
            _logger.LogInformation("Verification email sent to {Email} for user {CASId} from {Address}", InputEmail, casId, HttpContext.Connection.RemoteIpAddress);

            HttpContext.Session.SetString("pendingEmail", InputEmail);
            HttpContext.Session.SetInt32("verifyCode", code);
            VerifyCodeSent = true;
            return Page();
        }
        else // Verification code submitted
        {
            // Do not use InputEmail: can be modified by user (IDOR)
            if (HttpContext.Session.GetString("pendingEmail") != null && HttpContext.Session.GetInt32("verifyCode") == InputVerifyCode)
            {
                user.Email = HttpContext.Session.GetString("pendingEmail");
                user.EmailVerified = true;
                user.IPAtEmailVerify = HttpContext.Connection.RemoteIpAddress!.ToString();
                var task = _db.SaveChangesAsync();

                var ret = Redirect(SsoSignIn(sso, sig, new Dictionary<string, string?>()
                {
                    { "external_id", user.CasId },
                    { "name", user.Name },
                    { "email", user.Email },
                    { "custom.user_field_1", user.Org }
                }));

                _logger.LogInformation("Email verified for user {CASId} from {Address}", casId, HttpContext.Connection.RemoteIpAddress);
                await task;
                return ret;
            }
            else // Something goes wrong, wipe everything.
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
                HttpContext.Session.Remove("verifyCode");
                HttpContext.Session.Remove("pendingEmail");
                VerifyCodeSent = false;
                _logger.LogWarning("Invalid verification code for {CASId} and {Email} from {Address}", casId, InputEmail, HttpContext.Connection.RemoteIpAddress);
                return Page();
            }
        }
    }

    public async Task<IActionResult> OnGet(string? sso, string? sig)
    {
        string casId = User.FindFirst("id")!.Value;
        var user = (await _db.User.FindAsync(casId))!;
        if (ChangeEmail ?? false) // User wants to change email
        {
            user.Email = null;
            user.EmailVerified = false;
            await _db.SaveChangesAsync();
            return Page();
        }
        if (!user.EmailVerified) // No valid email
        {
            return Page();
        }

        var ret = SsoSignIn(sso!, sig!, new Dictionary<string, string?>()
            {
                { "external_id", user.CasId },
                { "name", user.Name },
                { "email", user.Email! },
                { "custom.user_field_1", user.Org }
            });
        _logger.LogInformation("User {CASId} logged in Discourse from {Address}", user.CasId, HttpContext.Connection.RemoteIpAddress);
        return Redirect(ret);
    }
}
