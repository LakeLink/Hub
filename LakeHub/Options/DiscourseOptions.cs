namespace LakeHub.Options;

public class DiscourseOptions
{
    public const string Key = "Discourse";
    public string? ConnectSecret { get; set; }
    public string? WebhookSecret { get; set; }
    public string? WeComBotKey { get; set; }
}