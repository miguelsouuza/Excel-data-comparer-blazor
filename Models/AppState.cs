using DataComparer.Models;

public class AppState
{
    public List<GenericRegistration> BaseA { get; set; } = new List<GenericRegistration>();
    public List<GenericRegistration> BaseB { get; set; } = new();
    public string ColunaId { get; set; } = string.Empty;
    public List<string> IdsA { get; set; } = new List<string>();
    public List<string> IdsB { get; set; } = new List<string>();
    public Dictionary<string, string> Mapeamento { get; set; } = new Dictionary<string, string>();    
    public Dictionary<string, string> MapeamentoUI { get; set; } = new Dictionary<string, string>();    
    public MemoryStream StreamA { get; set; }= new MemoryStream();
    public MemoryStream StreamB { get; set; }= new MemoryStream();
    public string NomeArquivoA { get; set; }= string.Empty;
    public string NomeArquivoB { get; set; }= string.Empty;
    public string DelimitadorCsv { get; set; } = ";";
    public bool ShowMappingHeader { get; set; } = false;
    public bool ShowMappingColumn { get; set; } = false;
    public string NomeArquivoExportacao { get; set; } = "resultado_alinhamento";

    public List<SheetMapping> MapeamentosPorAba { get; set; } = new();
    public Dictionary<string, SheetMapping> BaseBPorAba { get; set; } = new();
}
