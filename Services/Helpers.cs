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
    }

}
