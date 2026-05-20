using DataComparer.Interface;
using DataComparer.Models;

namespace DataComparer.Services
{
    public class DataQualityService : IDataQualityService
    {

        public ValidationResult ValidarRelacionamentos(List<GenericRegistration> registros)
        {
            var result = new ValidationResult();

            for (int i = 0; i < registros.Count; i++)
            {
                var row = registros[i];

                row.Campos.TryGetValue("DISTRITO", out var distrito);
                row.Campos.TryGetValue("SETOR", out var setor);

                if (!Helpers.PossuiMesmoPrefixo(distrito, setor, 3))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Tipo = "Relacionamento inválido",
                        ColunaPai = "DISTRITO",
                        ColunaFilho = "SETOR",
                        ValorPai = distrito ?? "",
                        ValorFilho = setor ?? "",
                        Linha = i + 1,
                        Mensagem =
                            $"SETOR {setor} não pertence ao DISTRITO {distrito}"
                    });
                }
            }

            return result;
        }        
    }
}
