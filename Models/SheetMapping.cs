namespace DataComparer.Models
{
    public class SheetMapping
    {
        public string NomeAbaA { get; set; }
        public string NomeAbaB { get; set; }
        public List<GenericRegistration> Registros { get; set; }
        public List<string> HeadersA { get; set; }
        public List<string> HeadersB { get; set; }

        public Dictionary<string, string> Mapping { get; set; }
    }
}
