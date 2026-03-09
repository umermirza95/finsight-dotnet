namespace Finsight.Commands
{
    public class WebhookEmailCommand
    {
        public string Subject { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty; 
        public string Html { get; set; } = string.Empty;
        public List<string> From { get; set; }=[];
        public List<string> To { get; set; }=[];
    }
}