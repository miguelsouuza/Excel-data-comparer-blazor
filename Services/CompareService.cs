using DataComparer.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public class CompareService : Helpers, ICompareService
{
    public CompareResult Compare(
        List<GenericRegistration> baseA,
        List<GenericRegistration> baseB,
        string colunaIdA,
        string colunaIdB,
        Dictionary<string, string> mapeamento)
    {
        var resultado = new CompareResult();
        var diferencas = new List<Difference>();
        var erros = new List<string>();

        // 🔴 Validações iniciais
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

        // 🔴 Detectar IDs duplicados na Base B
        var duplicadosB = baseB
            .Where(x => x.Campos.ContainsKey(colunaIdB))
            .GroupBy(x => Normalize(x.Campos[colunaIdB]))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicadosB.Any())
        {
            erros.Add($"IDs duplicados na Base B: {string.Join(", ", duplicadosB)}");
        }

        // 🔹 Criar índice da Base B
        var dictB = baseB
            .Where(x => x.Campos.ContainsKey(colunaIdB))
            .Select(x => new
            {
                Item = x,
                Id = Normalize(x.Campos[colunaIdB])
            })
            .Where(x => !string.IsNullOrEmpty(x.Id))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().Item);

        // 🔹 Comparação
        foreach (var itemA in baseA)
        {
            if (!itemA.Campos.ContainsKey(colunaIdA))
            {
                erros.Add("Registro sem ID na Base A");
                continue;
            }

            var idOriginal = itemA.Campos[colunaIdA];
            var id = Normalize(idOriginal);

            if (string.IsNullOrEmpty(id))
                continue;

            if (!dictB.ContainsKey(id))
                continue;

            var itemB = dictB[id];

            foreach (var map in mapeamento)
            {
                var colunaA = map.Key;
                var colunaB = map.Value;

                // 🔹 Pegando valores com TryGetValue (melhor performance)
                itemA.Campos.TryGetValue(colunaA, out var valorAOriginal);
                itemB.Campos.TryGetValue(colunaB, out var valorBOriginal);

                valorAOriginal ??= "";
                valorBOriginal ??= "";

                var valorANormalizado = Normalize(valorAOriginal);
                var valorBNormalizado = Normalize(valorBOriginal);

                if (!valorANormalizado.Equals(valorBNormalizado, StringComparison.OrdinalIgnoreCase))
                {
                    diferencas.Add(new Difference
                    {
                        Id = idOriginal, // 👈 mantém valor real
                        Campo = $"{colunaA} vs {colunaB}",
                        ValorA = valorAOriginal,
                        ValorB = valorBOriginal
                    });
                }
            }
        }

        // 🔹 Sets de IDs
        var idsA = baseA
            .Where(x => x.Campos.ContainsKey(colunaIdA))
            .Select(x => Normalize(x.Campos[colunaIdA]))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        var idsB = baseB
            .Where(x => x.Campos.ContainsKey(colunaIdB))
            .Select(x => Normalize(x.Campos[colunaIdB]))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        // 🔹 Resultado final
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