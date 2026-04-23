internal interface IFileService
{
    Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName);
    List<string> ObterAbas(Stream stream);
    Task<List<Dictionary<string, string>>> CarregarExcelPorAba(Stream stream, string nomeAba);
}