public class AppState
{
    public List<GenericRegistration> BaseA { get; set; } = new List<GenericRegistration>();
    public List<GenericRegistration> BaseB { get; set; } = new List<GenericRegistration>();
    public string ColunaId { get; set; } = string.Empty;
    public string ColunaA { get; set; } = string.Empty;
    public string ColunaB { get; set; } = string.Empty;
    public List<string> ColunasComparar { get; set; } = new List<string>();
    public Dictionary<string, string> Mapeamento { get; set; } = new Dictionary<string, string>();
}
