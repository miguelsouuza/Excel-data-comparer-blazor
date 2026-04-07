public interface IAutoMappingService
{
    Dictionary<string, string> GerarMapeamento(
        List<string> colunasA,
        List<string> colunasB);

    
}