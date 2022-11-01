using System.Text.Json.Nodes;

namespace LakeHub.Services
{
    public class WeComBotService
    {

        private readonly HttpClient _http;

        public WeComBotService(HttpClient httpClient)
        {
            _http = httpClient;
            _http.BaseAddress = new Uri("https://qyapi.weixin.qq.com/cgi-bin/");
        }
        public async Task<HttpResponseMessage> SendMarkdown(string key, string markdown)
        {
            var payload = new JsonObject
            {
                ["msgtype"] = "markdown",
                ["markdown"] = new JsonObject
                {
                    ["content"] = markdown
                }
            };

            var resp = await _http.PostAsync($"webhook/send?key={key}", JsonContent.Create(payload));
            return resp.EnsureSuccessStatusCode();
        }
    }
}
