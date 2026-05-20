using DataComparer.Models;

public interface IAppState
{
    List<GenericRegistration> BaseA { get; set; }
    List<GenericRegistration> BaseB { get; set; }
    string ColunaId { get; set; }
    string DelimitadorCsv { get; set; }
    List<string> IdsA { get; set; }
    List<string> IdsB { get; set; }
    Dictionary<string, string> Mapeamento { get; set; }
    Dictionary<string, SheetMapping> MapeamentosPorAba { get; set; }
    Dictionary<string, string> MapeamentoUI { get; set; }
    string NomeArquivoA { get; set; }
    string NomeArquivoB { get; set; }
    string NomeArquivoExportacao { get; set; }
    bool ShowMappingColumn { get; set; }
    bool ShowMappingHeader { get; set; }
    MemoryStream StreamA { get; set; }
    MemoryStream StreamB { get; set; }
    List<ValidationIssue> ValidationIssues { get; set; }
}