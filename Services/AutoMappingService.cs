using System.Text.RegularExpressions;

public class AutoMappingService : IAutoMappingService
{
    public Dictionary<string, string> GerarMapeamento(
        List<string> colunasA,
        List<string> colunasB)
    {
        var resultado = new Dictionary<string, string>();

        foreach (var colA in colunasA)
        {
            var melhor = colunasB
                .Select(colB => new
                {
                    Coluna = colB,
                    Score = Similaridade(colA, colB)
                })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor != null && melhor.Score > 0.5)
                resultado[colA] = melhor.Coluna;
        }

        return resultado;
    }

    private double Similaridade(string a, string b)
    {
        a = Normalizar(a);
        b = Normalizar(b);

        if (a == b) return 1;

        if (a.Contains(b) || b.Contains(a)) return 0.8;

        var pa = a.Split('_');
        var pb = b.Split('_');

        var inter = pa.Intersect(pb).Count();
        var total = pa.Union(pb).Count();

        return total == 0 ? 0 : (double)inter / total;
    }

    private string Normalizar(string t)
    {
        return Regex.Replace(t.ToUpper(), @"[^A-Z0-9_]", "");
    }
}