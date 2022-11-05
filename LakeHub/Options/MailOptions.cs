namespace LakeHub.Options
{
    public class MailOptions
    {
        public const string Key = "Mail";
        public string? SysEmail { get; set; }
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string? SmtpPassword { get; set; }
    }
}
