using OfficeOpenXml;

namespace DataComparer.Services
{
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

    // Gera um nome seguro para tabela Excel a partir do nome da aba
    private string MakeSafeTableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Table1";

        // remover caracteres inválidos e substituir espaços
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else if (char.IsWhiteSpace(ch)) sb.Append('_');
            // ignorar outros
        }

        var candidate = sb.ToString();
        if (string.IsNullOrWhiteSpace(candidate)) candidate = "Table1";

        // tabelas não podem começar com número — prefixar se necessário
        if (char.IsDigit(candidate[0])) candidate = "T_" + candidate;

        // limitar tamanho
        if (candidate.Length > 50) candidate = candidate.Substring(0, 50);

        // garantir nome único não é responsabilidade desta função
        // caller pode adicionar sufixo se necessário
        return candidate;
    }

    // Gera bytes CSV da base fornecida (primeira linha = headers)
    public byte[] GerarCsvBytes(List<GenericRegistration> baseDados)
    {
        if (baseDados == null || !baseDados.Any())
            return Array.Empty<byte>();

        var headers = baseDados.First().Campos.Keys.ToList();

        var sb = new System.Text.StringBuilder();

        // header
        sb.AppendLine(string.Join(',', headers.Select(h => EscapeCsv(h))));

        // linhas
        foreach (var row in baseDados)
        {
            var vals = headers.Select(h => row.Campos.TryGetValue(h, out var v) ? v : "");
            sb.AppendLine(string.Join(',', vals.Select(v => EscapeCsv(v))));
        }

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        // Add UTF8 BOM so Excel (Windows) recognizes encoding properly
        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        if (preamble != null && preamble.Length > 0)
        {
            var withBom = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, withBom, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, withBom, preamble.Length, contentBytes.Length);
            return withBom;
        }

        return contentBytes;
    }

    private string EscapeCsv(string s)
    {
        if (s == null) return "";
        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
        if (s.Contains(',') || s.Contains('\n') || s.Contains('\r') || s.Contains('"'))
            return $"\"{s}\"";
        return s;
    }

    // Gera bytes XLSX da base fornecida
    public byte[] GerarExcelBytes(List<GenericRegistration> baseDados)
    {
        if (baseDados == null || !baseDados.Any())
            return Array.Empty<byte>();

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Sheet1");

        var headers = baseDados.First().Campos.Keys.ToList();

        for (int c = 0; c < headers.Count; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
        }

        for (int r = 0; r < baseDados.Count; r++)
        {
            var row = baseDados[r];
            for (int c = 0; c < headers.Count; c++)
            {
                row.Campos.TryGetValue(headers[c], out var val);
                ws.Cells[r + 2, c + 1].Value = val ?? "";
            }
        }

        return package.GetAsByteArray();
    }

    // Gera um XLSX contendo todas as abas do arquivo original (mantendo nomes)
    // Se headersA for informado, cada aba será reescrita usando a ordem de headersA e o mapeamento fornecido.
    public async Task<byte[]> GerarExcelBytesMultiSheetAsync(byte[] originalWorkbookBytes, List<string> headersA, Dictionary<string, string> mapping, List<string>? desiredSheetNames = null)
    {
        if (originalWorkbookBytes == null || originalWorkbookBytes.Length == 0)
            return Array.Empty<byte>();
        // Para evitar perda de relações e fórmulas, modificamos o pacote original em memória
        // em vez de copiar worksheets para outro pacote. Assim preservamos fórmulas, relações e partes
        // dependentes (sharedStrings, relationships, etc.).
        using var ms = new MemoryStream(originalWorkbookBytes);
        using var package = new ExcelPackage(ms);

        var originalBytesCopy = originalWorkbookBytes; // keep reference

        using var outPackage = new ExcelPackage();

        using var inStream = new MemoryStream(originalBytesCopy);
        using var inPackage = new ExcelPackage(inStream);

        // Build list of table worksheets from the original B in the same order
        var tableSheets = inPackage.Workbook.Worksheets
            .Where(w => w.Tables != null && w.Tables.Count > 0)
            .ToList();

        // If desiredSheetNames provided (from Base A), create one output sheet per desired name,
        // filling with corresponding table from B by index when available; otherwise create empty sheet with headersA.
        if (desiredSheetNames != null && desiredSheetNames.Any())
        {
            for (int i = 0; i < desiredSheetNames.Count; i++)
            {
                var targetName = desiredSheetNames[i] ?? $"Sheet{i + 1}";

                // ensure unique name
                var candidateName = targetName;
                int suffix = 1;
                while (outPackage.Workbook.Worksheets.Any(x => x.Name.Equals(candidateName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    candidateName = targetName + "_" + suffix;
                    suffix++;
                }

                var wsOut = outPackage.Workbook.Worksheets.Add(candidateName);

                List<Dictionary<string, string>> rows = new();

                if (i < tableSheets.Count)
                {
                    var src = tableSheets[i];
                    // tentar ler diretamente da worksheet (inclui fallback para tables quando Dimension for null)
                    rows = ReadRowsFromWorksheet(src);
                    // se vazia, tentar leitura genérica que usa stream (fallback)
                    if ((rows == null || rows.Count == 0))
                        rows = await CarregarExcelPorAba(new MemoryStream(originalBytesCopy), src.Name);
                }

                List<string> outHeaders = (headersA != null && headersA.Any()) ? headersA.ToList() : rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                // write headers
                for (int c = 0; c < outHeaders.Count; c++)
                    wsOut.Cells[1, c + 1].Value = outHeaders[c];

                // write rows using mapping
                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    for (int c = 0; c < outHeaders.Count; c++)
                    {
                        var colA = outHeaders[c];
                        string sourceCol = colA;
                        if (mapping != null && mapping.TryGetValue(colA, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                            sourceCol = mapped;

                        row.TryGetValue(sourceCol, out var val);
                        wsOut.Cells[r + 2, c + 1].Value = val ?? "";
                    }
                }

                var totalRows = Math.Max(1, rows.Count + 1);
                if (outHeaders.Count > 0)
                {
                    var tableRange = wsOut.Cells[1, 1, totalRows, outHeaders.Count];
                    var safeTableName = MakeSafeTableName(candidateName);
                    try
                    {
                        wsOut.Tables.Add(tableRange, safeTableName);
                    }
                    catch
                    {
                        wsOut.Tables.Add(tableRange, "Table_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                    }
                }
            }

            if (outPackage.Workbook.Worksheets.Count == 0)
                return originalWorkbookBytes;

            return outPackage.GetAsByteArray();
        }

    // Tenta ler linhas diretamente a partir de uma worksheet, usando Dimension quando disponível
    // ou o endereço da primeira tabela (ListObject) quando Dimension for nulo.
    private List<Dictionary<string, string>> ReadRowsFromWorksheet(ExcelWorksheet ws)
    {
        var rows = new List<Dictionary<string, string>>();

        if (ws == null)
            return rows;

        int startRow, endRow, startCol, endCol;

        if (ws.Dimension != null)
        {
            startRow = ws.Dimension.Start.Row;
            endRow = ws.Dimension.End.Row;
            startCol = ws.Dimension.Start.Column;
            endCol = ws.Dimension.End.Column;
        }
        else if (ws.Tables != null && ws.Tables.Count > 0)
        {
            var addr = ws.Tables[0].Address;
            startRow = addr.Start.Row;
            endRow = addr.End.Row;
            startCol = addr.Start.Column;
            endCol = addr.End.Column;
        }
        else
        {
            return rows; // nada a ler
        }

        // ler headers
        var headers = new List<string>();
        for (int c = startCol; c <= endCol; c++)
        {
            var original = ws.Cells[startRow, c].Text;
            var normalizado = Normalize(original);
            var final = MapearColuna(normalizado);
            headers.Add(final);
        }

        // linhas de dados
        for (int r = startRow + 1; r <= endRow; r++)
        {
            var dict = new Dictionary<string, string>();
            bool allEmpty = true;
            for (int c = startCol; c <= endCol; c++)
            {
                var val = ws.Cells[r, c].Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(val)) allEmpty = false;
                dict[headers[c - startCol]] = val;
            }
            if (!allEmpty)
                rows.Add(dict);
        }

        return rows;
    }

        // Fallback: if no desired names provided, include all table sheets from B using their original names
        foreach (var src in tableSheets)
        {
            var candidateName = src.Name;
            int suffix = 1;
            while (outPackage.Workbook.Worksheets.Any(x => x.Name.Equals(candidateName, System.StringComparison.OrdinalIgnoreCase)))
            {
                candidateName = src.Name + "_" + suffix;
                suffix++;
            }

            var wsOut = outPackage.Workbook.Worksheets.Add(candidateName);
            var rows = ReadRowsFromWorksheet(src);
            if ((rows == null || rows.Count == 0))
                rows = await CarregarExcelPorAba(new MemoryStream(originalBytesCopy), src.Name);
            var outHeaders = (headersA != null && headersA.Any()) ? headersA.ToList() : rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

            for (int c = 0; c < outHeaders.Count; c++) wsOut.Cells[1, c + 1].Value = outHeaders[c];
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < outHeaders.Count; c++)
                {
                    var colA = outHeaders[c];
                    string sourceCol = colA;
                    if (mapping != null && mapping.TryGetValue(colA, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                        sourceCol = mapped;

                    row.TryGetValue(sourceCol, out var val);
                    wsOut.Cells[r + 2, c + 1].Value = val ?? "";
                }
            }

            var totalRows = Math.Max(1, rows.Count + 1);
            if (outHeaders.Count > 0)
            {
                var tableRange = wsOut.Cells[1, 1, totalRows, outHeaders.Count];
                var safeTableName = MakeSafeTableName(candidateName);
                try { wsOut.Tables.Add(tableRange, safeTableName); }
                catch { wsOut.Tables.Add(tableRange, "Table_" + Guid.NewGuid().ToString("N").Substring(0, 8)); }
            }
        }

        if (outPackage.Workbook.Worksheets.Count == 0)
            return originalWorkbookBytes;

        return outPackage.GetAsByteArray();
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