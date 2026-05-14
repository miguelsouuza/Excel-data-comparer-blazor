
using DataComparer.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataComparer.Services
{
    public class FileService : IFileService
    {
        private string MapearColuna(string coluna)
        {
            coluna = coluna?.ToUpper().Trim() ?? string.Empty;

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

        private List<string> ReadHeadersFromWorksheet(ExcelWorksheet ws)
        {
            var result = new List<string>();
            if (ws == null) return result;

            int startCol, endCol, headerRow;
            if (ws.Dimension != null)
            {
                startCol = ws.Dimension.Start.Column;
                endCol = ws.Dimension.End.Column;
                headerRow = ws.Dimension.Start.Row;
            }
            else if (ws.Tables != null && ws.Tables.Count > 0)
            {
                var addr = ws.Tables[0].Address;
                startCol = addr.Start.Column;
                endCol = addr.End.Column;
                headerRow = addr.Start.Row;
            }
            else
            {
                return result;
            }

            for (int c = startCol; c <= endCol; c++)
            {
                var original = ws.Cells[headerRow, c].Text;
                result.Add(Helpers.Normalize(original));
            }

            return result;
        }

        private double ComputeJaccard(List<string> a, List<string> b)
        {
            if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) return 1.0;
            if (a == null || a.Count == 0 || b == null || b.Count == 0) return 0.0;

            var sa = new HashSet<string>(a.Select(x => Helpers.NormalizeForCompare(x)));
            var sb = new HashSet<string>(b.Select(x => Helpers.NormalizeForCompare(x)));

            var inter = sa.Intersect(sb).Count();
            var uni = sa.Union(sb).Count();
            if (uni == 0) return 0.0;
            return (double)inter / uni;
        }

        public byte[] GerarCsvBytes(List<GenericRegistration> baseDados, string delimitador = ";")
        {
            var separador = delimitador == "\t"
                            ? '\t'
                            : delimitador[0];
            if (baseDados == null || !baseDados.Any())
                return Array.Empty<byte>();

            var headers = baseDados.First().Campos.Keys.ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(separador, headers.Select(h => Helpers.EscapeCsv(h, separador))));

            foreach (var row in baseDados)
            {
                var vals = headers.Select(h => row.Campos.TryGetValue(h, out var v) ? v : string.Empty);
                sb.AppendLine(string.Join(separador, vals.Select(v => Helpers.EscapeCsv(v, separador))));
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
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



        public byte[] GerarExcelBytes(List<GenericRegistration> baseDados)
        {
            if (baseDados == null || !baseDados.Any())
                return Array.Empty<byte>();

            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add("Sheet1");

            var headers = baseDados.First().Campos.Keys.ToList();
            for (int c = 0; c < headers.Count; c++)
                ws.Cells[1, c + 1].Value = headers[c];

            for (int r = 0; r < baseDados.Count; r++)
            {
                var row = baseDados[r];
                for (int c = 0; c < headers.Count; c++)
                {
                    row.Campos.TryGetValue(headers[c], out var val);
                    ws.Cells[r + 2, c + 1].Value = val ?? string.Empty;
                }
            }

            return package.GetAsByteArray();
        }

        public async Task<byte[]> GerarExcelBytesMultiSheetAsync(byte[] originalWorkbookBytes,
        List<SheetMapping> mappingsPorAba,
        List<string>? desiredSheetNames = null,
        List<List<string>>? desiredSheetHeaders = null)
        {
            if (originalWorkbookBytes == null || originalWorkbookBytes.Length == 0)
                return Array.Empty<byte>();

            using var inStream = new MemoryStream(originalWorkbookBytes);
            using var inPackage = new ExcelPackage(inStream);
            using var outPackage = new ExcelPackage();

            var tableSheets = inPackage.Workbook.Worksheets
                .Where(w => (w.Tables != null && w.Tables.Count > 0)
                            || (w.Dimension != null && (w.PivotTables == null || w.PivotTables.Count == 0)))
                .ToList();

            var usedTables = new HashSet<ExcelWorksheet>();

            // LOOP PRINCIPAL POR ABA
            if (desiredSheetNames != null && desiredSheetNames.Any())
            {
                for (int i = 0; i < desiredSheetNames.Count; i++)
                {
                    var targetName = string.IsNullOrWhiteSpace(desiredSheetNames[i])
                        ? $"Sheet{i + 1}"
                        : desiredSheetNames[i];

                    var wsOut = outPackage.Workbook.Worksheets.Add(Helpers.MakeUniqueSheetName(outPackage, targetName));

                    ExcelWorksheet src = null;

                    // Match por headers (similaridade)
                    if (desiredSheetHeaders != null && desiredSheetHeaders.Count > i)
                    {
                        var desiredHeaders = desiredSheetHeaders[i] ?? new List<string>();

                        double bestScore = 0;
                        ExcelWorksheet best = null;

                        foreach (var candidate in tableSheets.Where(t => !usedTables.Contains(t)))
                        {
                            var candidateHeaders = ReadHeadersFromWorksheet(candidate);
                            var score = ComputeJaccard(desiredHeaders, candidateHeaders);

                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = candidate;
                            }
                        }

                        if (best != null && bestScore >= 0.15)
                        {
                            src = best;
                            usedTables.Add(src);
                        }
                    }

                    //  fallback
                    if (src == null)
                    {
                        src = tableSheets.FirstOrDefault(t => !usedTables.Contains(t));
                        if (src != null) usedTables.Add(src);
                    }

                    List<Dictionary<string, string>> rows = new();

                    if (src != null)
                    {
                        rows = ReadRowsFromWorksheet(src);

                        if (rows == null || rows.Count == 0)
                            rows = await CarregarExcelPorAba(new MemoryStream(originalWorkbookBytes), src.Name);
                    }

                    // 🔹 buscar mapping da aba atual
                    var mappingPorAba = mappingsPorAba?
                        .FirstOrDefault(m =>
                        Helpers.NormalizeForCompare(m.NomeAbaB) ==
                        Helpers.NormalizeForCompare(src?.Name ?? "")
                        );

                    // headers da aba
                    var outHeaders = mappingPorAba?.HeadersA != null && mappingPorAba.HeadersA.Any()
                    ? mappingPorAba.HeadersA
                    : rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                    // mapping da aba
                    var mappingFinal = mappingPorAba?.Mapping;

                    WriteSheetData(wsOut, outHeaders, rows, mappingFinal);
                }

                if (outPackage.Workbook.Worksheets.Count == 0)
                    return originalWorkbookBytes;

                return outPackage.GetAsByteArray();
            }

            //  fallback geral (sem desired)
            foreach (var fsrc in tableSheets.Where(t => !usedTables.Contains(t)))
            {
                var fwsOut = outPackage.Workbook.Worksheets.Add(Helpers.MakeUniqueSheetName(outPackage, fsrc.Name));

                var frows = ReadRowsFromWorksheet(fsrc);
                if (frows == null || frows.Count == 0)
                    frows = await CarregarExcelPorAba(new MemoryStream(originalWorkbookBytes), fsrc.Name);

                var outHeaders = frows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                // 🔹 buscar mapping da aba atual
                var mappingPorAba = mappingsPorAba?
                    .FirstOrDefault(m =>
                    Helpers.NormalizeForCompare(m.NomeAbaB) ==
                    Helpers.NormalizeForCompare(fsrc?.Name ?? "")
                    );

                // headers da aba
                var foutHeaders = mappingPorAba?.HeadersA != null && mappingPorAba.HeadersA.Any()
                ? mappingPorAba.HeadersA
                : frows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                // mapping da aba
                var fmappingFinal = mappingPorAba?.Mapping;

                WriteSheetData(fwsOut, foutHeaders, frows, fmappingFinal);
            }

            if (outPackage.Workbook.Worksheets.Count == 0)
                return originalWorkbookBytes;

            return outPackage.GetAsByteArray();
        }

        private void WriteSheetData(ExcelWorksheet wsOut, List<string> outHeaders, List<Dictionary<string, string>> rows, Dictionary<string, string> mapping)
        {
            for (int c = 0; c < outHeaders.Count; c++)
                wsOut.Cells[1, c + 1].Value = outHeaders[c];

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < outHeaders.Count; c++)
                {
                    var colA = outHeaders[c];
                    var sourceCol = colA;
                    if (mapping != null && mapping.TryGetValue(colA, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                        sourceCol = mapped;

                    row.TryGetValue(sourceCol, out var val);
                    wsOut.Cells[r + 2, c + 1].Value = val ?? string.Empty;
                }
            }
            wsOut.Cells.AutoFitColumns();
        }

        private List<Dictionary<string, string>> ReadRowsFromWorksheet(ExcelWorksheet ws)
        {
            var rows = new List<Dictionary<string, string>>();
            if (ws == null) return rows;

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
                return rows;
            }

            var headers = new List<string>();
            for (int c = startCol; c <= endCol; c++)
            {
                var original = ws.Cells[startRow, c].Text;
                headers.Add(MapearColuna(Helpers.Normalize(original)));
            }

            for (int r = startRow + 1; r <= endRow; r++)
            {
                var dict = new Dictionary<string, string>();
                bool allEmpty = true;
                for (int c = startCol; c <= endCol; c++)
                {
                    var val = ws.Cells[r, c].Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(val)) allEmpty = false;
                    dict[headers[c - startCol]] = val;
                }
                if (!allEmpty) rows.Add(dict);
            }

            return rows;
        }

        public List<string> ObterAbas(Stream stream)
        {
            try
            {
                stream.Position = 0;
                using var package = new ExcelPackage(stream);
                return package.Workbook.Worksheets
                    .Select(ws => ws.Name)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<Dictionary<string, string>>> CarregarExcelPorAba(Stream stream, string nomeAba)
        {
            stream.Position = 0;

            using var package = new ExcelPackage(stream);

            var ws = package.Workbook.Worksheets
                .FirstOrDefault(x =>
                    x.Name.Trim().Equals(nomeAba.Trim(), StringComparison.OrdinalIgnoreCase));

            if (ws == null)
                throw new Exception($"A aba '{nomeAba}' não foi encontrada.");

            if (ws.Dimension == null)
                throw new Exception($"A aba '{nomeAba}' está vazia.");

            int colunas = ws.Dimension.Columns;
            int linhas = ws.Dimension.Rows;

            var headers = new List<string>();

            for (int col = 1; col <= colunas; col++)
            {
                var original = ws.Cells[1, col].Text;
                headers.Add(MapearColuna(Helpers.Normalize(original)));
            }

            var lista = new List<Dictionary<string, string>>();

            for (int lin = 2; lin <= linhas; lin++)
            {
                var dict = new Dictionary<string, string>();
                bool linhaVazia = true;

                for (int col = 1; col <= colunas; col++)
                {
                    var valor = ws.Cells[lin, col].Text?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(valor))
                        linhaVazia = false;

                    dict[headers[col - 1]] = valor;
                }

                if (!linhaVazia)
                    lista.Add(dict);
            }

            return lista;
        }

        public async Task<List<GenericRegistration>> CarregarTxt(Stream stream)
        {
            var lista = new List<GenericRegistration>();
            using var reader = new StreamReader(stream);
            var linhas = new List<string>();
            string? linha;
            while ((linha = await reader.ReadLineAsync()) != null)
                linhas.Add(linha);

            if (!linhas.Any()) return lista;
            var separador = Helpers.DetectSeparator(linhas[0]);
            var headers = linhas[0].Split(separador).Select(h => MapearColuna(Helpers.Normalize(h))).ToArray();

            foreach (var linhaRow in linhas.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linhaRow)) continue;
                var valores = linhaRow.Split(separador);
                if (valores.Length != headers.Length) continue;
                var registro = new GenericRegistration();
                for (int i = 0; i < headers.Length; i++) registro.Campos[headers[i]] = valores[i]?.Trim() ?? string.Empty;
                lista.Add(registro);
            }

            return lista;
        }

        public async Task<List<GenericRegistration>> CarregarArquivoAsync(Stream stream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".xlsx" => (await CarregarExcelPorAba(stream, ObterAbas(stream).First())).Select(d => new GenericRegistration { Campos = d }).ToList(),
                ".csv" or ".txt" => await CarregarTxt(stream),
                _ => throw new Exception("Formato não suportado")
            };
        }
    }

}
