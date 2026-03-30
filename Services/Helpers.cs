namespace DataComparer.Services
{
    public class Helpers
    {
        protected static char DetectarSeparador(string linha)
        {
            if (linha.Contains(";")) return ';';
            if (linha.Contains("|")) return '|';
            if (linha.Contains(",")) return ',';

            return ';'; // padrão
        }
        protected static string Normalizar(string texto)
        {
            return texto?
                .Replace("\uFEFF", "") // remove BOM
                .Trim()
                .ToUpper() ?? "";
        }
    }

}
