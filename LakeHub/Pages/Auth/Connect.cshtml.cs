using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using LakeHub.Services;

namespace LakeHub.Pages.Auth;

[Authorize]
public class ConnectModel : PageModel
{
    private readonly ILogger<ConnectModel> _logger;
    private readonly ICASRestProtocol _cas;

    public ConnectModel(ILogger<ConnectModel> logger, ICASRestProtocol cas)
    {
        _logger = logger;
        _cas = cas;
    }

    public IActionResult OnPost()
    {
        return Page();
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

    public async Task<IActionResult> OnGetAsync(string serviceUrl)
    {
        await HttpContext.Session.LoadAsync();
        try
        {
            var cred = new CASUsernamePasswordCredential(User.FindFirst("id")!.Value, User.FindFirst("password")!.Value);
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