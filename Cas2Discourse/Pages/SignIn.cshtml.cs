using Cas2Discourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cas2Discourse.Pages
{
    public class SignInModel : PageModel
    {
        private readonly ILogger<SignInModel> _logger;
        private readonly ICASRestProtocol _cas;
        private readonly DiscourseSsoProtocol _sso;

        [BindProperty]
        public string? InputCasId { get; set; }

        [BindProperty]
        public string? InputEmail { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string? InputPassword { get; set; }

        public SignInModel(ILogger<SignInModel> logger, ICASRestProtocol cas, DiscourseSsoProtocol sso)
        {
            _logger = logger;
            _cas = cas;
            _sso = sso;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost(string? sso, string? sig)
        {

            JsonDocument json = await _cas.Users(new CASUsernamePasswordCredential(InputCasId!, InputPassword!));
            var JsonPrincipleAttribs = json.RootElement.GetProperty("authentication").GetProperty("successes").GetProperty("RestAuthenticationHandler").GetProperty("principal").GetProperty("attributes");

            var ret = _sso.Sign(sso!, sig!, new Dictionary<string, string?>()
            {
                { "external_id", InputCasId },
                { "name", JsonPrincipleAttribs.GetProperty("name").GetString()! },
                { "email", InputEmail! },
                { "require_activation", "true" },
                // user_field_{id}, id can be found in the url of each custom field or through api response
                { "custom.user_field_1", JsonPrincipleAttribs.GetProperty("organization").GetString()! }
            });
            _logger.LogInformation("User {CASId} logged in Discourse from {Address}", InputCasId, HttpContext.Connection.RemoteIpAddress);
            return Redirect(ret);
        }
    }
}
