namespace DataComparer.Models
{
    public class ValidationIssue
    {
        public string Tipo { get; set; } = "";
        public string Coluna { get; set; } = "";
        public string ColunaPai { get; set; } = "";
        public string ColunaFilho { get; set; } = "";
        public string Valor { get; set; } = "";
        public string ValorPai { get; set; } = "";
        public string ValorFilho { get; set; } = "";
        public int Linha { get; set; }
        public string Mensagem { get; set; } = "";
        public string Aba { get; set; } = "";
    }
}
