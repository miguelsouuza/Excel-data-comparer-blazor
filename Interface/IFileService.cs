using DataComparer.Models;

internal interface IFileService
{
    Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName);
    List<string> ObterAbas(Stream stream);
    Task<List<Dictionary<string, string>>> CarregarExcelPorAba(Stream stream, string nomeAba);
    Task<List<GenericRegistration>> CarregarTxt(Stream stream);
    Task<byte[]> GerarExcelBytesMultiSheetAsync(byte[] originalWorkbookBytes,
        List<SheetMapping> mappingsPorAba,
        List<string>? desiredSheetNames = null,
        List<List<string>>? desiredSheetHeaders = null);

    byte[] GerarCsvBytes(List<GenericRegistration> baseDados, string delimitador = ";");

    byte[] GerarExcelBytes(List<GenericRegistration> baseDados);
}