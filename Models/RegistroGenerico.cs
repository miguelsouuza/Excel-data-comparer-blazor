public class RegistroGenerico
{
    public Dictionary<string, string> Campos { get; set; }
    public RegistroGenerico() => Campos = new Dictionary<string, string>();
}