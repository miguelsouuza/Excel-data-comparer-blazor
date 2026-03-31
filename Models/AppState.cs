public class AppState
{
    public List<RegistroGenerico> BaseA { get; set; } = new List<RegistroGenerico>();
    public List<RegistroGenerico> BaseB { get; set; } = new List<RegistroGenerico>();
    public string ColunaId { get; set; } = string.Empty;
    public string ColunaA { get; set; } = string.Empty;
    public string ColunaB { get; set; } = string.Empty;
    public List<string> ColunasComparar { get; set; } = new List<string>();
    public Dictionary<string, string> Mapeamento { get; set; } = new Dictionary<string, string>();
}
