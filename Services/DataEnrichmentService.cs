using DataComparer.Interface;
using DataComparer.Models;

namespace DataComparer.Services
{
    public class DataEnrichmentService : IDataEnrichmentService
    {
        public List<GenericRegistration> AplicarEnriquecimento(
            List<GenericRegistration> registros,
            SheetMapping config)
        {
            if (registros == null || !registros.Any())
                return new();

            var resultado = registros
                .Select(r => new GenericRegistration
                {
                    Campos = new Dictionary<string, string>(r.Campos)
                })
                .ToList();

            foreach (var row in resultado)
            {
                foreach (var coluna in config.HeadersA)
                {
                    // garante existência
                    if (!row.Campos.ContainsKey(coluna))
                    {
                        row.Campos[coluna] = "";
                    }

                    //---------------------------------------------------
                    // 1) TEMPLATE PERSONALIZADO
                    //---------------------------------------------------
                    if (config.TemplatesPersonalizados != null
                        && config.TemplatesPersonalizados.TryGetValue(coluna, out var template)
                        && !string.IsNullOrWhiteSpace(template))
                    {
                        var valorTemplate = template;

                        foreach (var campo in row.Campos)
                        {
                            valorTemplate = valorTemplate.Replace(
                                $"{{{campo.Key}}}",
                                campo.Value ?? "");
                        }

                        row.Campos[coluna] = valorTemplate;

                        continue;
                    }

                    //---------------------------------------------------
                    // 2) VALOR FIXO
                    //---------------------------------------------------
                    if (config.ValoresFixos != null
                        && config.ValoresFixos.TryGetValue(coluna, out var valorFixo)
                        && !string.IsNullOrWhiteSpace(valorFixo))
                    {
                        row.Campos[coluna] = valorFixo;

                        continue;
                    }

                    //---------------------------------------------------
                    // 3) COPIAR COLUNA DA BASE A
                    //---------------------------------------------------
                    if (config.ColunasFixasDaBaseA != null
                        && config.ColunasFixasDaBaseA.TryGetValue(coluna, out var colunaOrigemA)
                        && !string.IsNullOrWhiteSpace(colunaOrigemA))
                    {
                        if (row.Campos.TryGetValue(colunaOrigemA, out var valorA))
                        {
                            row.Campos[coluna] = valorA ?? "";
                        }

                        continue;
                    }

                    //---------------------------------------------------
                    // 4) COPIAR COLUNA DA BASE B
                    //---------------------------------------------------
                    if (config.ColunasFixasDaBaseB != null
                        && config.ColunasFixasDaBaseB.TryGetValue(coluna, out var colunaOrigemB)
                        && !string.IsNullOrWhiteSpace(colunaOrigemB))
                    {
                        if (row.Campos.TryGetValue(colunaOrigemB, out var valorB))
                        {
                            row.Campos[coluna] = valorB ?? "";
                        }

                        continue;
                    }
                }
            }

            //---------------------------------------------------
            // 5) INFERÊNCIA INTELIGENTE
            //---------------------------------------------------
            AplicarInferencia(resultado);

            //---------------------------------------------------
            // 6) VALIDAÇÕES
            //---------------------------------------------------
            ValidarConsistencia(resultado);

            return resultado;
        }

        private void AplicarInferencia(List<GenericRegistration> registros)
        {
            var grupos = registros
                .Where(x =>
                    x.Campos.ContainsKey("DISTRITO")
                    && x.Campos.ContainsKey("SETOR"))
                .GroupBy(x => x.Campos["DISTRITO"]);

            foreach (var grupo in grupos)
            {
                var setorMaisComum = grupo
                    .Select(x => x.Campos["SETOR"])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .GroupBy(x => x)
                    .OrderByDescending(x => x.Count())
                    .FirstOrDefault()?.Key;

                if (string.IsNullOrWhiteSpace(setorMaisComum))
                    continue;

                foreach (var row in grupo)
                {
                    if (string.IsNullOrWhiteSpace(row.Campos["SETOR"]))
                    {
                        row.Campos["SETOR"] = setorMaisComum;
                    }
                }
            }
        }

        private void ValidarConsistencia(List<GenericRegistration> registros)
        {
            var inconsistencias = registros
                .Where(x =>
                    x.Campos.ContainsKey("DISTRITO")
                    && x.Campos.ContainsKey("SETOR"))
                .GroupBy(x => x.Campos["DISTRITO"])
                .Where(g =>
                    g.Select(x => x.Campos["SETOR"])
                     .Distinct()
                     .Count() > 1)
                .ToList();

            foreach (var inconsistencia in inconsistencias)
            {
                Console.WriteLine(
                    $"Distrito inconsistente: {inconsistencia.Key}");
            }
        }
    }
}
