public interface ICompareService
{
    CompareResult Compare(List<RegistroGenerico> baseA,
    List<RegistroGenerico> baseB,
    string colunaIdA,
    string colunaIdB,
    Dictionary<string, string> mapeamento);
}