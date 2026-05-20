using DataComparer.Models;

namespace DataComparer.Interface
{
    public interface IDataEnrichmentService
    {
        List<GenericRegistration> AplicarEnriquecimento(List<GenericRegistration> registros,SheetMapping config);
    }
}