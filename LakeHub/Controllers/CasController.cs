using LakeHub.Pages.Auth;
using LakeHub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Security.Claims;

namespace LakeHub.Controllers
{
    [Route("/Auth/Cas")]
    [ApiController]
    public class CasController : ControllerBase
    {
        private readonly ILogger<CasController> _logger;
        private readonly ICASRestProtocol _cas;

        public CasController(ILogger<CasController> logger, ICASRestProtocol cas)
        {
            _logger = logger;
            _cas = cas;
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

        [HttpGet("Connect")]
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
