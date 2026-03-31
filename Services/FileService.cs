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

    private async Task<List<RegistroGenerico>> CarregarTxt(Stream stream)
    {
        var lista = new List<RegistroGenerico>();

        using var reader = new StreamReader(stream);
        var linhas = new List<string>();

        string? linha;
        while ((linha = await reader.ReadLineAsync()) != null)
        {
            linhas.Add(linha);
        }

        if (linhas.Count == 0) return lista;

        var separador = DetectarSeparador(linhas[0]);

        var headers = linhas[0]
            .Split(separador)
            .Select(h => MapearColuna(Normalizar(h)))
            .ToArray();

        foreach (var linhaRow in linhas.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linhaRow))
                continue;

            var valores = linhaRow.Split(separador);

            if (valores.Length != headers.Length)
                continue;

            var registro = new RegistroGenerico();

            for (int i = 0; i < headers.Length; i++)
            {
                var valor = valores[i]?.Trim() ?? "";

                registro.Campos[headers[i]] = valor;
            }

            lista.Add(registro);
        }

        return lista;
    }

    private async Task<List<RegistroGenerico>> CarregarExcel(Stream stream)
    {
        var lista = new List<RegistroGenerico>();

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
            var headerNormalizado = MapearColuna(Normalizar(headerOriginal));

            headers.Add(headerNormalizado);
        }

        for (int lin = 2; lin <= linhas; lin++)
        {
            var registro = new RegistroGenerico();

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

    public async Task<List<RegistroGenerico>> CarregarArquivoAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        return ext switch
        {
            ".xlsx" => await CarregarExcel(stream),
            ".csv" or ".txt" => await CarregarTxt(stream),
            _ => throw new Exception("Formato não suportado")
        };
    }
}