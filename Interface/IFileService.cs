public interface IFileService
{
    Task<List<RegistroGenerico>> CarregarArquivoAsync(Stream stream, string fileName);
}