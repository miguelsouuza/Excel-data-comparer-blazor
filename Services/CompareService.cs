using DataComparer.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public class CompareService : Helpers, ICompareService
{
    public CompareResult Compare(
        List<GenericRegistration> baseA,
        List<GenericRegistration> baseB,
        List<string> idsA,
        List<string> idsB,
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

        if (idsA == null || !idsA.Any())
            erros.Add("IDs da Base A não informados");

        if (idsB == null || !idsB.Any())
            erros.Add("IDs da Base B não informados");

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
            .Where(x => !string.IsNullOrEmpty(GerarChave(x, idsB)))
            .GroupBy(x => GerarChave(x, idsB))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicadosB.Any())
        {
            erros.Add($"IDs duplicados na Base B: {string.Join(", ", duplicadosB)}");
        }

        // 🔹 Criar índice da Base B
        var dictB = baseB
            .Where(x => !string.IsNullOrEmpty(GerarChave(x, idsB)))
            .Select(x => new
            {
                Item = x,
                Id = GerarChave(x, idsB)
            })
            .Where(x => !string.IsNullOrEmpty(x.Id))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().Item);

        // 🔹 Comparação
        foreach (var itemA in baseA)
        {
            if (string.IsNullOrEmpty(GerarChave(itemA, idsA)))
            {
                erros.Add("Registro sem ID na Base A");
                continue;
            }

            var id = GerarChave(itemA, idsA);
            var idOriginal = id;

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
        var idsBaseA = baseA
            .Where(x => !string.IsNullOrEmpty(GerarChave(x, idsA)))
            .Select(x => GerarChave(x, idsA))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        var idsBaseB = baseB
            .Where(x => !string.IsNullOrEmpty(GerarChave(x, idsB)))
            .Select(x => GerarChave(x, idsB))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        // 🔹 Resultado final
        return new CompareResult
        {
            Diferencas = diferencas,
            ApenasA = idsBaseA.Except(idsBaseB).ToList(),
            ApenasB = idsBaseB.Except(idsBaseA).ToList(),
            EmAmbas = idsBaseA.Intersect(idsBaseB).Count(),
            Total = idsBaseA.Union(idsBaseB).Count(),
            Erros = erros
        };
    }
    private string GerarChave(
    GenericRegistration registro,
    List<string> colunas)
    {
        return string.Join("|",
            colunas.Select(col =>
            {
                registro.Campos.TryGetValue(col, out var valor);

                return Normalize(valor ?? "");
            }));
    }
}