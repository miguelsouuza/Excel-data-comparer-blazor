namespace DataComparer.Services
{
    public class Helpers
    {
        protected static char DetectSeparator(string linha)
        {
            if (linha.Contains(";")) return ';';
            if (linha.Contains("|")) return '|';
            if (linha.Contains(",")) return ',';

            return ';'; // padrão
        }
        protected static string Normalize(string texto)
        {
            return texto?
                .Replace("\uFEFF", "") // remove BOM
                .Replace(".", "")
                .Replace("-", "")
                .Replace("/", "")
                .Trim()
                .ToUpper() ?? "";
        }
        private string SanitizeWorksheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Sheet";

            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

            foreach (var c in invalidChars)
            {
                name = name.Replace(c.ToString(), "");
            }

            name = name.Trim();

            if (name.Length > 31)
                name = name.Substring(0, 31);

            return name;
        }
    }

}
