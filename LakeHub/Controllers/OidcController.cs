using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using LakeHub.Models;

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
            string casId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DbUser user = (await _db.User.FindAsync(casId))!;
            var identity = new ClaimsIdentity("OpenIddict");
            identity.AddClaim(Claims.Subject, User.FindFirstValue(ClaimTypes.NameIdentifier),
                Destinations.AccessToken, Destinations.IdentityToken);
            identity.AddClaim(Claims.Name, User.FindFirstValue(ClaimTypes.Name),
                Destinations.IdentityToken);
            identity.AddClaim("cas:organization", User.FindFirstValue("cas:organization"));

            identity.AddClaim(Claims.Email, user.Email!,
                Destinations.IdentityToken);
            identity.AddClaim(Claims.EmailVerified, user.EmailVerified.ToString(),
                Destinations.IdentityToken);
            var principal = new ClaimsPrincipal(identity);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
