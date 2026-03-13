namespace Finsight.Commands
{
    public class WebhookEmailCommand
    {
        public string Subject { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public EmailGroup From { get; set; } = new();
        public List<string> Recipients { get; set; } = [];
    }

    public class EmailGroup
    {
        public List<EmailContact> Value { get; set; } = [];
    }

    public class EmailContact
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}