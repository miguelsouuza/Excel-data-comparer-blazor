using DataComparer.Models;

internal interface IFileService
{
    Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName);
    List<string> ObterAbas(Stream stream);
    Task<List<Dictionary<string, string>>> CarregarExcelPorAba(Stream stream, string nomeAba);
    Task<List<GenericRegistration>> CarregarTxt(Stream stream);
    Task<byte[]> GerarExcelBytesMultiSheetAsync(
         byte[] originalWorkbookBytes,
         Dictionary<string, SheetMapping> sheets);

    byte[] GerarCsvBytes(List<GenericRegistration> baseDados, string delimitador = ";");

    byte[] GerarExcelBytes(Dictionary<string, SheetMapping> mapeamentosPorAba, string nomeArquivo);
}