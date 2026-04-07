using DataComparer.Services;
using OfficeOpenXml;

public class FileService : Helpers, IFileService
{
    // 🔥 Mapeamento de colunas (PADRÃO)
    private string MapearColuna(string coluna)
    {
        coluna = coluna.ToUpper().Trim();

        return coluna switch
        {
            "CNESID" => "CNES",
            "CÓDIGO CNES" => "CNES",
            "CODIGO CNES" => "CNES",

            "NM_CLIENTE" => "NOME",
            "NOME CLIENTE" => "NOME",

            "GOVERNMENTID" => "CNPJ",
            "CPF_CNPJ" => "CNPJ",

            _ => coluna
        };
    }

    private async Task<List<GenericRegistration>> CarregarTxt(Stream stream)
    {
        var lista = new List<GenericRegistration>();

        using var reader = new StreamReader(stream);
        var linhas = new List<string>();

        string? linha;
        while ((linha = await reader.ReadLineAsync()) != null)
        {
            linhas.Add(linha);
        }

        if (linhas.Count == 0) return lista;

        var separador = DetectSeparator(linhas[0]);

        var headers = linhas[0]
            .Split(separador)
            .Select(h => MapearColuna(Normalize(h)))
            .ToArray();

        foreach (var linhaRow in linhas.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linhaRow))
                continue;

            var valores = linhaRow.Split(separador);

            if (valores.Length != headers.Length)
                continue;

            var registro = new GenericRegistration();

            for (int i = 0; i < headers.Length; i++)
            {
                var valor = valores[i]?.Trim() ?? "";

                registro.Campos[headers[i]] = valor;
            }

            lista.Add(registro);
        }

        return lista;
    }

    private async Task<List<GenericRegistration>> CarregarExcel(Stream stream)
    {
        var lista = new List<GenericRegistration>();

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        ExcelPackage.License.SetNonCommercialOrganization("BlazorApp");

        using var package = new ExcelPackage(memoryStream);
        var ws = package.Workbook.Worksheets.FirstOrDefault();

        if (ws?.Dimension == null)
            return lista;

        int colunas = ws.Dimension.Columns;
        int linhas = ws.Dimension.Rows;

        var headers = new List<string>();

        for (int col = 1; col <= colunas; col++)
        {
            var headerOriginal = ws.Cells[1, col].Text;
            var headerNormalizado = MapearColuna(Normalize(headerOriginal));

            headers.Add(headerNormalizado);
        }

        for (int lin = 2; lin <= linhas; lin++)
        {
            var registro = new GenericRegistration();

            for (int col = 1; col <= colunas; col++)
            {
                var nomeColuna = headers[col - 1];
                var valor = ws.Cells[lin, col].Text?.Trim() ?? "";

                registro.Campos[nomeColuna] = valor;
            }

            lista.Add(registro);
        }

        return lista;
    }

    public async Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        return ext switch
        {
            ".xlsx" => await CarregarExcel(stream),
            ".csv" or ".txt" => await CarregarTxt(stream),
            _ => throw new Exception("Formato não suportado")
        };
    }

    public async Task<List<string>> GetSheetNamesAsync(Stream fileStream)
    {
        var sheets = new List<string>();

        using (var package = new ExcelPackage(fileStream))
        {
            foreach (var ws in package.Workbook.Worksheets)
            {
                sheets.Add(ws.Name);
            }
        }

        return sheets;
    }

    public async Task<List<Dictionary<string, string>>> ReadExcelAsync(
    Stream fileStream,
    string sheetName)
    {
        var result = new List<Dictionary<string, string>>();

        using (var package = new ExcelPackage(fileStream))
        {
            var worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null)
                throw new Exception($"A aba '{sheetName}' não foi encontrada.");

            var colCount = worksheet.Dimension.Columns;
            var rowCount = worksheet.Dimension.Rows;

            var headers = new List<string>();

            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text);
            }

            for (int row = 2; row <= rowCount; row++)
            {
                var dict = new Dictionary<string, string>();

                for (int col = 1; col <= colCount; col++)
                {
                    dict[headers[col - 1]] = worksheet.Cells[row, col].Text;
                }

                result.Add(dict);
            }
        }

        return result;
    }
}