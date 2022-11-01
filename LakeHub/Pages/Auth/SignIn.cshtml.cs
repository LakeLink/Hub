using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Refit;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using LakeHub.Models;
using LakeHub.Services;

namespace LakeHub.Pages.Auth;

public class SignInModel : PageModel
{
    private readonly ILogger<SignInModel> _logger;
    private readonly ICASRestProtocol _cas;
    private readonly LakeHubContext _db;

    public SignInModel(ILogger<SignInModel> logger, ICASRestProtocol cas, LakeHubContext dbCtx)
    {
        _logger = logger;
        _cas = cas;
        _db = dbCtx;
    }

    [BindProperty]
    public string? InputCASId { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    public string? InputPassword { get; set; }

    [BindProperty]
    [Display(Name = "Trust this device")]
    public bool TrustThisDevice { get; set; } = true;

    public bool LoginSuccess { get; set; }

    public void OnGet()
    {
    }
    //https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/cookie
    public async Task<IActionResult> OnPostAsync(string? ReturnUrl)
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            JsonDocument json = await _cas.Users(new CASUsernamePasswordCredential(InputCASId!, InputPassword!));
            var JsonPrincipleAttribs = json.RootElement.GetProperty("authentication").GetProperty("successes").GetProperty("RestAuthenticationHandler").GetProperty("principal").GetProperty("attributes");

            LoginSuccess = true;

            var user = await _db.User.FindAsync(InputCASId);
            if (user == null)
            {
                user = new DbUser(InputCASId!, JsonPrincipleAttribs.GetProperty("name").GetString()!)
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
                new Claim("id", InputCASId!),
                new Claim("password", InputPassword!),
                new Claim("name", user.Name!),
                // new Claim("pycc", JsonPrincipleAttribs.GetProperty("pycc").GetString()!),
                new Claim("org", user.Org!)
            };

            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, "id", "role");
            claimsIdentity.AddClaims(claims);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = TrustThisDevice,
                // https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs#L416
                RedirectUri = ReturnUrl ?? Url.Page("/Index")
            };

            var ret = SignIn(new ClaimsPrincipal(claimsIdentity), authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {CASId} signed in from {Address}", InputCASId, HttpContext.Connection.RemoteIpAddress);
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
                    _logger.LogWarning("{Address} tried to sign in a nonexistent account {CASId}.", HttpContext.Connection.RemoteIpAddress, InputCASId);
                }
                else
                {
                    ModelState.AddModelError("InputPassword", "Incorrect password.");
                    _logger.LogWarning("{Address} tried to sign in as {CASId} with wrong password.", HttpContext.Connection.RemoteIpAddress, InputCASId);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Gateway returned an unexpected error.");
            }
            return Page();
        }
        // catch (Exception e)
        // {
        //     _logger.LogError(e, "Unexpected error with user {UserID}", UserID);

        //     //ModelState.AddModelError(string.Empty, "An unexpected internal error has occured.");
        //     //return Page();
        // }
    }
}
