using DataComparer.Services;

public class CompareService : Helpers, ICompareService
{
    public CompareResult Compare(
    List<RegistroGenerico> baseA,
    List<RegistroGenerico> baseB,
    string colunaIdA,
    string colunaIdB,
    Dictionary<string, string> mapeamento)
    {
        var diferencas = new List<Diferenca>();
        var erros = new List<string>();

        if (baseA == null || !baseA.Any())
            erros.Add("Base A está vazia");

        if (baseB == null || !baseB.Any())
            erros.Add("Base B está vazia");

        if (string.IsNullOrWhiteSpace(colunaIdA) || string.IsNullOrWhiteSpace(colunaIdB))
            erros.Add("Coluna ID não informada corretamente");

        if (mapeamento == null || !mapeamento.Any())
            erros.Add("Mapeamento não informado");

        if (erros.Any())
        {
            return new CompareResult
            {
                Diferencas = diferencas,
                Erros = erros
            };
        }

        // 🔹 Criar índice da base B
        var dictB = baseB
            .Where(x => x.Campos.ContainsKey(colunaIdB))
            .Select(x => new
            {
                Item = x,
                Id = Normalizar(x.Campos[colunaIdB])
            })
            .Where(x => !string.IsNullOrEmpty(x.Id))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().Item);

        // 🔹 Comparação
        foreach (var itemA in baseA)
        {
            if (!itemA.Campos.ContainsKey(colunaIdA))
                continue;

            var id = Normalizar(itemA.Campos[colunaIdA]);

            if (string.IsNullOrEmpty(id))
                continue;

            if (!dictB.ContainsKey(id))
                continue;

            var itemBComparar = dictB[id];

            foreach (var map in mapeamento)
            {
                var colunaA = map.Key;
                var colunaB = map.Value;

                var valorA = itemA.Campos.ContainsKey(colunaA)
                    ? Normalizar(itemA.Campos[colunaA])
                    : "";

                var valorB = itemBComparar.Campos.ContainsKey(colunaB)
                    ? Normalizar(itemBComparar.Campos[colunaB])
                    : "";

                if (!valorA.Equals(valorB, StringComparison.OrdinalIgnoreCase))
                {
                    diferencas.Add(new Diferenca
                    {
                        Id = id,
                        Campo = $"{colunaA} vs {colunaB}",
                        ValorA = valorA,
                        ValorB = valorB
                    });
                }
            }
        }

        // 🔹 Sets de IDs
        var idsA = baseA
            .Where(x => x.Campos.ContainsKey(colunaIdA))
            .Select(x => Normalizar(x.Campos[colunaIdA]))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        var idsB = baseB
            .Where(x => x.Campos.ContainsKey(colunaIdB))
            .Select(x => Normalizar(x.Campos[colunaIdB]))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        return new CompareResult
        {
            Diferencas = diferencas,
            ApenasA = idsA.Except(idsB).ToList(),
            ApenasB = idsB.Except(idsA).ToList(),
            EmAmbas = idsA.Intersect(idsB).Count(),
            Total = idsA.Union(idsB).Count(),
            Erros = erros
        };
    }
}