using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MYAcademy_IdentityEmailProject.Helpers
{
   
     
    public static class HtmlSanitizer
    {
        private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "div", "span", "b", "strong", "i", "em", "u", "s", "strike", "font",
            "ul", "ol", "li", "h1", "h2", "h3", "h4", "blockquote", "a", "hr", "sub", "sup"
        };

        private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase) { "br", "hr" };

        
        private static readonly Regex DangerousBlocks = new(
            @"<(script|style|iframe|object|embed|noscript|template|svg|math)\b[^>]*>.*?</\1\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex Comments = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)([^>]*)>", RegexOptions.Compiled);
        private static readonly Regex AttrRegex = new(@"([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+))", RegexOptions.Compiled);
        private static readonly Regex SafeCssValue = new(@"^[#a-zA-Z0-9(),.%\s-]{1,60}$", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedCss = new(StringComparer.OrdinalIgnoreCase)
        {
            "color", "background-color", "text-align", "font-weight", "font-style", "text-decoration"
        };

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            html = Comments.Replace(html, string.Empty);
            html = DangerousBlocks.Replace(html, string.Empty);

            var sb = new StringBuilder();
            int last = 0;
            foreach (Match m in TagRegex.Matches(html))
            {
                sb.Append(EncodeText(html.Substring(last, m.Index - last)));
                last = m.Index + m.Length;

                bool closing = m.Groups[1].Value == "/";
                string tag = m.Groups[2].Value.ToLowerInvariant();
                if (!AllowedTags.Contains(tag)) continue;                    

                if (closing)
                {
                    if (!VoidTags.Contains(tag)) sb.Append("</").Append(tag).Append('>');
                    continue;
                }

                sb.Append('<').Append(tag);
                foreach (Match a in AttrRegex.Matches(m.Groups[3].Value))
                {
                    string name = a.Groups[1].Value.ToLowerInvariant();
                    string value = WebUtility.HtmlDecode(a.Groups[2].Success ? a.Groups[2].Value : a.Groups[3].Success ? a.Groups[3].Value : a.Groups[4].Value).Trim();

                    if (name == "href" && tag == "a" && IsSafeUrl(value))
                        sb.Append(" href=\"").Append(WebUtility.HtmlEncode(value)).Append("\" target=\"_blank\" rel=\"noopener noreferrer\"");
                    else if (name == "style")
                    {
                        var css = CleanStyle(value);
                        if (css.Length > 0) sb.Append(" style=\"").Append(WebUtility.HtmlEncode(css)).Append('"');
                    }
                    else if (name == "color" && tag == "font" && SafeCssValue.IsMatch(value))
                        sb.Append(" color=\"").Append(WebUtility.HtmlEncode(value)).Append('"');
                    else if (name == "align" && (value is "left" or "center" or "right"))
                        sb.Append(" align=\"").Append(value).Append('"');
                    
                }
                sb.Append(VoidTags.Contains(tag) ? " />" : ">");
            }
            sb.Append(EncodeText(html.Substring(last)));
            return sb.ToString().Trim();
        }

        
        private static string EncodeText(string text) => text.Replace("<", "&lt;").Replace(">", "&gt;");

        private static bool IsSafeUrl(string url) =>
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

        private static string CleanStyle(string style)
        {
            var parts = new List<string>();
            foreach (var decl in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = decl.Split(':', 2);
                if (kv.Length != 2) continue;
                var prop = kv[0].Trim();
                var val = kv[1].Trim();
                if (AllowedCss.Contains(prop) && SafeCssValue.IsMatch(val) && !val.Contains("url", StringComparison.OrdinalIgnoreCase) && !val.Contains("expression", StringComparison.OrdinalIgnoreCase))
                    parts.Add($"{prop.ToLowerInvariant()}: {val}");
            }
            return string.Join("; ", parts);
        }

        
        public static string ToPlainText(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var text = DangerousBlocks.Replace(Comments.Replace(html, string.Empty), string.Empty);
            text = Regex.Replace(text, @"<(br|/p|/div|/li|/h[1-6]|/blockquote|hr)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"[ \t ]+", " ");
            text = Regex.Replace(text, @"\s*\n\s*", "\n");
            return text.Trim();
        }

        
        public static string Snippet(string? html, int max = 120)
        {
            var text = Regex.Replace(ToPlainText(html), @"\s+", " ");
            return text.Length > max ? text[..max].TrimEnd() + "…" : text;
        }
    }
}
