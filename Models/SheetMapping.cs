namespace DataComparer.Models
{
    public class SheetMapping
    {
        public string NomeAbaA { get; set; } = "";
        public string NomeAbaB { get; set; } = "";

        public List<string> HeadersA { get; set; } = new();
        public List<string> HeadersB { get; set; } = new();

        public Dictionary<string, string> Mapping { get; set; } = new();

        public List<GenericRegistration> Registros { get; set; } = new();

        public Dictionary<string, string> ColunasFixasDaBaseA { get; set; } = new();

        public Dictionary<string, string> ValoresFixos { get; set; } = new();
    }
}
