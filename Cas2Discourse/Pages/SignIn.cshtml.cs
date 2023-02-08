using Cas2Discourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Refit;
using System.ComponentModel.DataAnnotations;
using System.Net;
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
        [DataType(DataType.EmailAddress)]
        public string? InputEmail { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string? InputPassword { get; set; }

        public bool WrongId { get; set; } = false;
        public bool WrongPassword { get; set; } = false;
        public bool UnknownError { get; set; } = false;

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
            try
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
            catch (ApiException e)
            {
                if (e.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (e.Content != null && e.Content.Contains("AccountNotFoundException"))
                    {
                        _logger.LogWarning("{Address} tried to sign in a nonexistent account {CASId}.", HttpContext.Connection.RemoteIpAddress, InputCasId);

                        InputCasId = "";
                        WrongId = true;
                    }
                    else
                    {
                        _logger.LogWarning("{Address} tried to sign in as {CASId} with wrong password.", HttpContext.Connection.RemoteIpAddress, InputCasId);

                        InputPassword = "";
                        WrongPassword = true;
                    }
                }
                else
                {
                    UnknownError = true;
                    _logger.LogWarning("Gateway returned an unexpected error | {Address} {CASId}", HttpContext.Connection.RemoteIpAddress, InputCasId);
                }
                return Page();
            }
        }
    }
}
