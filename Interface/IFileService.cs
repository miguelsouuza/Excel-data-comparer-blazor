public interface IFileService
{
    Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName);
}