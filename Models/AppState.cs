using DataComparer.Models;

public class AppState : IAppState
{
    public List<GenericRegistration> BaseA { get; set; } = new();

    public List<GenericRegistration> BaseB { get; set; } = new();

    public string ColunaId { get; set; } = string.Empty;

    public List<string> IdsA { get; set; } = new();

    public List<string> IdsB { get; set; } = new();

    public Dictionary<string, string> Mapeamento { get; set; } = new();

    public Dictionary<string, string> MapeamentoUI { get; set; } = new();

    public MemoryStream StreamA { get; set; } = new();

    public MemoryStream StreamB { get; set; } = new();

    public string NomeArquivoA { get; set; } = string.Empty;

    public string NomeArquivoB { get; set; } = string.Empty;

    public string DelimitadorCsv { get; set; } = ";";

    public bool ShowMappingHeader { get; set; } = false;

    public bool ShowMappingColumn { get; set; } = false;

    public string NomeArquivoExportacao { get; set; } = string.Empty;

    public Dictionary<string, SheetMapping> MapeamentosPorAba { get; set; } = new();

    public List<ValidationIssue> ValidationIssues { get; set; } = new();
}