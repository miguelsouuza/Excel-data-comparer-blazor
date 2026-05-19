using System.Collections.Generic;

public class AlignmentResult
{
    public List<GenericRegistration> Alinhada { get; set; } = new();
    public Dictionary<string, string> Mapeamento { get; set; } = new();

    public List<(string ColunaA, string ColunaB)> Renomeadas { get; set; } = new();
    public List<string> Mantidas { get; set; } = new();
    public List<string> Removidas { get; set; } = new();
    public List<(string ColunaA, string FonteB)> Incluidas { get; set; } = new();

    public List<string> HeadersA { get; set; } = new();
    public List<string> HeadersB { get; set; } = new();

    public List<string> HeadersOriginaisB { get; set; } = new();
}
