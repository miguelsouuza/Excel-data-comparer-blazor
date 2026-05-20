using DataComparer.Models;

namespace DataComparer.Interface
{
    public interface IDataQualityService
    {
        ValidationResult ValidarRelacionamentos(List<GenericRegistration> registros);
    }
}