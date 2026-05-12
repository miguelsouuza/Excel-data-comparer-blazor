public class AppState
{
    public List<GenericRegistration> BaseA { get; set; } = new List<GenericRegistration>();
    public List<GenericRegistration> BaseB { get; set; } = new List<GenericRegistration>();
    public string ColunaId { get; set; } = string.Empty;
    public List<string> IdsA { get; set; } = new List<string>();
    public List<string> IdsB { get; set; } = new List<string>();
    public List<string> ColunasComparar { get; set; } = new List<string>();
    public Dictionary<string, string> Mapeamento { get; set; } = new Dictionary<string, string>();    
    public MemoryStream StreamA { get; set; }
    public MemoryStream StreamB { get; set; }
    public string NomeArquivoA { get; set; }
    public string NomeArquivoB { get; set; }    
}
