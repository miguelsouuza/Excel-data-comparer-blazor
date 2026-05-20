using DataComparer.Interface;
using DataComparer.Models;

namespace DataComparer.Services
{
    public class ValidationService : IValidationService
    {
        public List<ValidationIssue> Validar(
            List<GenericRegistration> registros,
            string nomeAba = "")
        {
            var issues = new List<ValidationIssue>();

            if (registros == null || !registros.Any())
                return issues;

            //---------------------------------------------------
            // 1) CAMPOS VAZIOS
            //---------------------------------------------------

            for (int i = 0; i < registros.Count; i++)
            {
                var row = registros[i];

                foreach (var campo in row.Campos)
                {
                    if (string.IsNullOrWhiteSpace(campo.Value))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Tipo = "Campo vazio",
                            Coluna = campo.Key,
                            Valor = "",
                            Linha = i + 2,
                            Aba = nomeAba,
                            Mensagem =
                                $"A coluna '{campo.Key}' está vazia."
                        });
                    }
                }
            }

            //---------------------------------------------------
            // 2) DUPLICIDADE
            //---------------------------------------------------

            foreach (var coluna in registros
                .First()
                .Campos.Keys)
            {
                var duplicados = registros
                    .Where(x =>
                        x.Campos.ContainsKey(coluna)
                        && !string.IsNullOrWhiteSpace(
                            x.Campos[coluna]))
                    .GroupBy(x => x.Campos[coluna])
                    .Where(g => g.Count() > 1);

                foreach (var dup in duplicados)
                {
                    issues.Add(new ValidationIssue
                    {
                        Tipo = "Duplicidade",
                        Coluna = coluna,
                        Valor = dup.Key,
                        Aba = nomeAba,
                        Mensagem =
                            $"Valor duplicado encontrado em '{coluna}'."
                    });
                }
            }

            //---------------------------------------------------
            // 3) RELAÇÃO DISTRITO x SETOR
            //---------------------------------------------------

            if (registros.First().Campos.ContainsKey("DISTRITO")
                && registros.First().Campos.ContainsKey("SETOR"))
            {
                var grupos = registros
                    .GroupBy(x =>
                        x.Campos["DISTRITO"]);

                foreach (var grupo in grupos)
                {
                    var setores = grupo
                        .Select(x => x.Campos["SETOR"])
                        .Distinct()
                        .ToList();

                    if (setores.Count > 1)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Tipo = "Relacionamento inconsistente",
                            Coluna = "SETOR",
                            Valor = grupo.Key,
                            Aba = nomeAba,
                            Mensagem =
                                $"O distrito '{grupo.Key}' possui múltiplos setores."
                        });
                    }
                }
            }

            //---------------------------------------------------
            // 4) TAMANHO DIFERENTE
            //---------------------------------------------------

            foreach (var coluna in registros
                .First()
                .Campos.Keys)
            {
                var tamanhos = registros
                    .Where(x =>
                        x.Campos.ContainsKey(coluna)
                        && !string.IsNullOrWhiteSpace(
                            x.Campos[coluna]))
                    .Select(x => x.Campos[coluna].Length)
                    .ToList();

                if (!tamanhos.Any())
                    continue;

                var media = tamanhos.Average();

                foreach (var row in registros)
                {
                    if (!row.Campos.TryGetValue(
                        coluna,
                        out var valor))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(valor))
                        continue;

                    if (Math.Abs(valor.Length - media) > 10)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Tipo = "Tamanho suspeito",
                            Coluna = coluna,
                            Valor = valor,
                            Aba = nomeAba,
                            Mensagem =
                                $"Valor fora do padrão esperado."
                        });
                    }
                }
            }

            return issues;
        }
    }
}
