public class CompareResult
{
    public List<Difference> Diferencas { get; set; }
    public List<string> ApenasA { get; set; }
    public List<string> ApenasB { get; set; }
    public int EmAmbas { get; set; }
    public int Total { get; set; }
    public List<string> Erros { get; set; }
}