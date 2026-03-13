using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;

namespace Finsight.Utils
{
    public static class EmailCleaner
    {
        public static string CleanEmailHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodesToRemove = doc.DocumentNode.SelectNodes("//style|//script|//head|//meta|//link|//comment()");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                    node.Remove();
            }
            string text = doc.DocumentNode.InnerText;
            text = HttpUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }
    }
}