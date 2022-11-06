using LakeHub.Options;
using LakeHub.Pages.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LakeHub.Areas.Auth.Controllers
{
    [Area("Auth")]
    public class DiscourseController : Controller
    {
        private readonly ILogger<DiscourseController> _logger;
        private readonly DiscourseOptions _options;
        private readonly LakeHubContext _db;

        public DiscourseController(ILogger<DiscourseController> logger, IOptionsMonitor<DiscourseOptions> options, LakeHubContext dbCtx)
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

        [Authorize("EmailRequired")]
        public async Task<IActionResult> Connect(string? sso, string? sig)
        {
            string casId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = (await _db.User.FindAsync(casId))!;
            //if (!user.EmailVerified) // No valid email
            //{
            //    return RedirectToAction("Email", "Cas", new
            //    {
            //        ReturnUrl = Request.
            //    });
            //}

            var ret = SsoSignIn(sso!, sig!, new Dictionary<string, string?>()
            {
                { "external_id", user.CasId },
                { "name", user.Name },
                { "email", user.Email! },
                // user_field_{id}, id can be found in the url of each custom field or through api response
                { "custom.user_field_1", user.Org }
            });
            _logger.LogInformation("User {CASId} logged in Discourse from {Address}", user.CasId, HttpContext.Connection.RemoteIpAddress);
            return Redirect(ret);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
