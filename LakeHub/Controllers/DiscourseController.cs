using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LakeHub.Options;
using LakeHub.Services;

namespace LakeHub.Controllers
{
    [Route("_Api/1/Webhook/[controller]")]
    [ApiController]
    public class DiscourseController : ControllerBase
    {
        private readonly ILogger<DiscourseController> _logger;
        private readonly string _secret;
        private readonly WeComBotService _bot;
        private readonly string _botKey;

        public DiscourseController(ILogger<DiscourseController> logger, IOptionsMonitor<DiscourseOptions> options, WeComBotService bot)
        {
            _logger = logger;
            _secret = options.CurrentValue.WebhookSecret!;
            _botKey = options.CurrentValue.WeComBotKey!;
            _bot = bot;
        }

        [HttpPost]
        public async Task<StatusCodeResult> Post(
            [FromHeader(Name = "X-Discourse-Event-Type")] string eventType
            )
        {
            byte[] body;
            using (var ms = new MemoryStream(2048))
            {
                await Request.Body.CopyToAsync(ms);
                body = ms.ToArray();
            }
            if (_secret != null)
            {
                using HMACSHA256 hmac = new(Encoding.ASCII.GetBytes(_secret));
                if ("sha256=" + Convert.ToHexString(hmac.ComputeHash(body)) != Request.Headers["X-Discourse-Event-Signature"].First().ToUpper())
                {
                    return BadRequest();
                }
            }
            switch (eventType)
            {
                case "reviewable":
                    JsonDocument json = JsonDocument.Parse(body);
                    var e = json.RootElement.GetProperty("reviewable");
                    var md = $"# {e.GetProperty("type")}\ntarget_url: [{e.GetProperty("target_url")}]({e.GetProperty("target_url")})\ncreated_at: {e.GetProperty("created_at")}";
                    _logger.LogInformation(md);
                    await _bot.SendMarkdown(_botKey, md);
                    return Ok();
                default:
                    return BadRequest();
            }
        }
    }
}
