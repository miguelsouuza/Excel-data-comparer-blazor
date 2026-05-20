using DataComparer.Models;

namespace DataComparer.Interface
{
    public interface IValidationService
    {
        List<ValidationIssue> Validar(List<GenericRegistration> registros, string nomeAba = "");
    }
}