public interface ICompareService
{
    CompareResult Compare(List<GenericRegistration> baseA,
    List<GenericRegistration> baseB,
    string colunaIdA,
    string colunaIdB,
    Dictionary<string, string> mapeamento);
}