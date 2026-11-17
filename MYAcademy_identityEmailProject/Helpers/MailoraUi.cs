using System.Globalization;

namespace MYAcademy_IdentityEmailProject.Helpers
{
  
    public static class MailoraUi
    {
        public static readonly CultureInfo Tr = new("tr-TR");
        private static readonly string[] AvatarColors = { "av-lav", "av-blue", "av-mint", "av-rose", "av-peach", "av-sun" };

       
        public static string Initials(string? nameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(nameOrEmail)) return "?";
            var source = nameOrEmail.Contains('@') ? nameOrEmail.Split('@')[0] : nameOrEmail;
            var parts = source.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var letters = string.Concat(parts.Take(2).Select(p => p[0]));
            return letters.Length == 0 ? "?" : letters.ToUpper(Tr);
        }

        
        public static string AvatarColor(string? key)
        {
            var h = 0;
            foreach (var c in (key ?? string.Empty).ToLowerInvariant()) h = (h * 31 + c) & 0x7fffffff;
            return AvatarColors[h % AvatarColors.Length];
        }

        
        public static string ShortTime(DateTime d)
        {
            var today = DateTime.Today;
            if (d.Date == today) return d.ToString("HH:mm", Tr);
            if (d.Date == today.AddDays(-1)) return "Dün";
            if (d.Year == today.Year) return d.ToString("d MMM", Tr);
            return d.ToString("dd.MM.yyyy", Tr);
        }

        public static string LongDate(DateTime d) => d.ToString("d MMMM yyyy, dddd HH:mm", Tr);

        public static string FullName(string? name, string? surname, string? fallback = null)
        {
            var full = $"{name} {surname}".Trim();
            return string.IsNullOrEmpty(full) ? (fallback ?? string.Empty) : full;
        }

        
        public static string LabelClass(string? label) => label switch
        {
            "İş" => "tag-is",
            "Kişisel" => "tag-kisisel",
            "Fatura" => "tag-fatura",
            _ => "tag-sistem"
        };
    }
}
