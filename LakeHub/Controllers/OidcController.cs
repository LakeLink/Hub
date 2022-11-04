using LakeHub.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace LakeHub.Controllers
{
    [Route("/Auth/Oidc")]
    [ApiController]
    public class OidcController : ControllerBase
    {
        private readonly LakeHubContext _db;
        private readonly ILogger<OidcController> _logger;
        public OidcController(ILogger<OidcController> logger, LakeHubContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("Authorize")]
        [HttpPost("Authorize")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()!;

            string casId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DbUser user = (await _db.User.FindAsync(casId))!;
            var identity = new ClaimsIdentity("OpenIddict");
            identity.AddClaim(Claims.Subject, User.FindFirstValue(ClaimTypes.NameIdentifier),
                Destinations.AccessToken, Destinations.IdentityToken);
            identity.AddClaim(Claims.Name, User.FindFirstValue(ClaimTypes.Name),
                Destinations.AccessToken);
            identity.AddClaim("cas:organization", User.FindFirstValue("cas:organization"),
                Destinations.AccessToken);

            identity.AddClaim(Claims.Email, user.Email!,
                Destinations.AccessToken);
            identity.AddClaim(Claims.EmailVerified, user.EmailVerified.ToString(),
                Destinations.AccessToken);
            var principal = new ClaimsPrincipal(identity);

            // OAuth2 doesn't have scopes.
            // Oidc request: should always have openid scope
            if (request.GetScopes().Length == 0)
            {
                principal.SetScopes(new[]
                {
                    Scopes.Profile,
                    Scopes.Email
                });
            }
            else principal.SetScopes(request.GetScopes());

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
