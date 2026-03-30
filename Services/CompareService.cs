public class CompareService : ICompareService
{
    public CompareResult Compare(
        List<RegistroGenerico> baseA,
        List<RegistroGenerico> baseB,
        string colunaId,
        List<string> colunasComparar)
    {
        var diferencas = new List<Diferenca>();
        var erros = new List<string>();

        var dictB = new Dictionary<string, RegistroGenerico>();

        foreach (var itemB in baseB)
        {
            if (!itemB.Campos.ContainsKey(colunaId))
            {
                erros.Add("Registro sem ID na base B");
                continue;
            }

            var id = itemB.Campos[colunaId];

            if (string.IsNullOrWhiteSpace(id))
            {
                erros.Add("ID vazio na base B");
                continue;
            }

            if (!dictB.ContainsKey(id))
                dictB.Add(id, itemB);
            else
                erros.Add($"ID duplicado na base B: {id}");
        }

        foreach (var itemA in baseA)
        {
            if (!itemA.Campos.ContainsKey(colunaId))
                continue;

            var id = itemA.Campos[colunaId];

            if (!dictB.ContainsKey(id))
                continue;

            var itemBComparar = dictB[id];

            foreach (var coluna in colunasComparar)
            {
                var valorA = itemA.Campos.ContainsKey(coluna) ? itemA.Campos[coluna] : "";
                var valorB = itemBComparar.Campos.ContainsKey(coluna) ? itemBComparar.Campos[coluna] : "";

                if (!valorA.Equals(valorB, StringComparison.OrdinalIgnoreCase))
                {
                    diferencas.Add(new Diferenca
                    {
                        Id = id,
                        Campo = coluna,
                        ValorA = valorA,
                        ValorB = valorB
                    });
                }
            }
        }

        var idsA = baseA
            .Where(x => x.Campos.ContainsKey(colunaId))
            .Select(x => x.Campos[colunaId])
            .ToHashSet();

        var idsB = baseB
            .Where(x => x.Campos.ContainsKey(colunaId))
            .Select(x => x.Campos[colunaId])
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