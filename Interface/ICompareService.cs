public interface ICompareService
{
    CompareResult Compare(List<RegistroGenerico> baseA, List<RegistroGenerico> baseB, string colunaId, List<string> colunasComparar);
}