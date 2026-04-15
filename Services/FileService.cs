using DataComparer.Services;
using OfficeOpenXml;

public class FileService : Helpers, IFileService
{
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

    // 🔥 LISTAR ABAS
    public List<string> ObterAbas(Stream stream)
    {
        stream.Position = 0;

        using var package = new ExcelPackage(stream);

        return package.Workbook.Worksheets
            .Select(ws => ws.Name)
            .ToList();
    }

    // 🔥 LER ABA (VERSÃO CORRETA)
    public async Task<List<Dictionary<string, string>>> CarregarExcelPorAba(
        Stream stream,
        string nomeAba)
    {
        stream.Position = 0;

        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[nomeAba];

        if (ws?.Dimension == null)
            return new List<Dictionary<string, string>>();

        int colunas = ws.Dimension.Columns;
        int linhas = ws.Dimension.Rows;

        var headers = new List<string>();

        // 🔥 HEADERS NORMALIZADOS
        for (int col = 1; col <= colunas; col++)
        {
            var original = ws.Cells[1, col].Text;
            var normalizado = Normalize(original);
            var final = MapearColuna(normalizado);

            headers.Add(final);
        }

        var lista = new List<Dictionary<string, string>>();

        // 🔹 LINHAS
        for (int lin = 2; lin <= linhas; lin++)
        {
            var dict = new Dictionary<string, string>();

            bool linhaVazia = true;

            for (int col = 1; col <= colunas; col++)
            {
                var valor = ws.Cells[lin, col].Text?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(valor))
                    linhaVazia = false;

                dict[headers[col - 1]] = valor;
            }

            if (linhaVazia)
                continue;

            lista.Add(dict);
        }

        return lista;
    }

    // 🔥 TXT/CSV
    public async Task<List<GenericRegistration>> CarregarTxt(Stream stream)
    {
        var lista = new List<GenericRegistration>();

        using var reader = new StreamReader(stream);
        var linhas = new List<string>();

        string? linha;
        while ((linha = await reader.ReadLineAsync()) != null)
        {
            linhas.Add(linha);
        }

        if (!linhas.Any())
            return lista;

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
                registro.Campos[headers[i]] = valores[i]?.Trim() ?? "";
            }

            lista.Add(registro);
        }

        return lista;
    }

    // 🔥 MÉTODO PRINCIPAL
    public async Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        return ext switch
        {
            ".xlsx" => (await CarregarExcelPorAba(stream, ObterAbas(stream).First()))
                .Select(d => new GenericRegistration { Campos = d })
                .ToList(),

            ".csv" or ".txt" => await CarregarTxt(stream),

            _ => throw new Exception("Formato não suportado")
        };
    }
}