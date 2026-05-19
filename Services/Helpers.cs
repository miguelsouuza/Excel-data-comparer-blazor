using OfficeOpenXml;
using System.Text;

namespace DataComparer.Services
{
    public class Helpers
    {
        public static char DetectSeparator(string linha)
        {
            if (linha.Contains(";")) return ';';
            if (linha.Contains("|")) return '|';
            if (linha.Contains(",")) return ',';

            return ';'; // padrão
        }
        public static string Normalize(string texto)
        {
            return texto?
                .Replace("\uFEFF", "") // remove BOM
                .Replace(".", "")
                .Replace("-", "")
                .Replace("/", "")
                .Trim()
                .ToUpper() ?? "";
        }
        public static string SanitizeWorksheetName(string name)
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

        public static string SafeFileName(string name, string extension)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "arquivo";

            // remove extensão existente
            name = Path.GetFileNameWithoutExtension(name);

            // remove caracteres inválidos
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            // evita nome vazio
            if (string.IsNullOrWhiteSpace(name))
                name = "arquivo";

            // garante extensão correta
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return name + extension;
        }
        public static string NormalizeForCompare(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var t = s.ToUpperInvariant().Trim();
            var sb = new StringBuilder();
            foreach (var ch in t)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else sb.Append(' ');
            }
            return sb.ToString().Replace(" ", "").Trim();
        }

        public static string EscapeCsv(string s, char separador)
        {
            if (s == null)
                return string.Empty;

            if (s.Contains('"'))
                s = s.Replace("\"", "\"\"");

            if (s.Contains(separador)
                || s.Contains('\n')
                || s.Contains('\r')
                || s.Contains('"'))
            {
                return $"\"{s}\"";
            }

            return s;
        }

        public static string MakeSafeTableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Table1";

            var sb = new System.Text.StringBuilder();
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
                else if (char.IsWhiteSpace(ch)) sb.Append('_');
            }

            var candidate = sb.ToString();
            if (string.IsNullOrWhiteSpace(candidate)) candidate = "Table1";
            if (char.IsDigit(candidate[0])) candidate = "T_" + candidate;
            if (candidate.Length > 50) candidate = candidate.Substring(0, 50);
            return candidate;
        }

        public static string MakeUniqueSheetName(ExcelPackage pkg, string baseName)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

            var candidate = baseName;

            foreach (var ch in invalidChars)
            {
                candidate = candidate.Replace(ch.ToString(), "");
            }

            candidate = candidate.Trim();

            if (candidate.Length > 31)
                candidate = candidate.Substring(0, 31);

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = "Sheet";

            return candidate;
        }
    }

}
