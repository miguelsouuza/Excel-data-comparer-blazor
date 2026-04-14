internal interface IHeaderCompareService
{
    HeaderCompareResult Comparar(List<string> colunasA, List<string> colunasB);
}