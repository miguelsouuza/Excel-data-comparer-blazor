public interface ICompareService
{
    CompareResult Compare(List<GenericRegistration> baseA,
    List<GenericRegistration> baseB,
    List<string> idsA,
    List<string> idsB,
    Dictionary<string, string> mapeamento);
}