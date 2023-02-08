using Refit;
using System.Text.Json;

namespace Cas2Discourse.Services;

public interface ICASRestProtocol
{
    [Post("/cas/v1/users")]
    Task<JsonDocument> Users([Body(BodySerializationMethod.UrlEncoded)] CASUsernamePasswordCredential credential);

    [Post("/cas/v1/tickets")]
    Task<ApiResponse<string>> Tickets([Body(BodySerializationMethod.UrlEncoded)] CASUsernamePasswordCredential credential);

    [Post("/cas/v1/tickets/{tgt_ticket}")]
    Task<ApiResponse<string>> Tickets(string tgt_ticket, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
}

public class CASUsernamePasswordCredential
{
    public string username { get; set; }
    public string password { get; set; }

    public CASUsernamePasswordCredential(string _username, string _password)
    {
        username = _username;
        password = _password;
    }
}
