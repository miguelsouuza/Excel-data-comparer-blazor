
using System;
using OfficeOpenXml;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataComparer.Models;


namespace DataComparer.Services
{
    public class FileService : Helpers, IFileService
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
                result.Add(Normalize(original));
            }

            return result;
        }

        private double ComputeJaccard(List<string> a, List<string> b)
        {
            if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) return 1.0;
            if (a == null || a.Count == 0 || b == null || b.Count == 0) return 0.0;

            var sa = new HashSet<string>(a.Select(x => NormalizeForCompare(x)));
            var sb = new HashSet<string>(b.Select(x => NormalizeForCompare(x)));

            var inter = sa.Intersect(sb).Count();
            var uni = sa.Union(sb).Count();
            if (uni == 0) return 0.0;
            return (double)inter / uni;
        }

        private string NormalizeForCompare(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var t = s.ToUpperInvariant().Trim();
            var sb = new StringBuilder();
            foreach (var ch in t)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else sb.Append(' ');
            }
            return sb.ToString().Replace(" ", "").Trim();
        }

        private string MakeSafeTableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Table1";

            var sb = new System.Text.StringBuilder();
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
                else if (char.IsWhiteSpace(ch)) sb.Append('_');
            }

            var candidate = sb.ToString();
            if (string.IsNullOrWhiteSpace(candidate)) candidate = "Table1";
            if (char.IsDigit(candidate[0])) candidate = "T_" + candidate;
            if (candidate.Length > 50) candidate = candidate.Substring(0, 50);
            return candidate;
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
            sb.AppendLine(string.Join(separador, headers.Select(h => EscapeCsv(h, separador))));

            foreach (var row in baseDados)
            {
                var vals = headers.Select(h => row.Campos.TryGetValue(h, out var v) ? v : string.Empty);
                sb.AppendLine(string.Join(separador, vals.Select(v => EscapeCsv(v, separador))));
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

        private string EscapeCsv(string s, char separador)
        {
            if (s == null)
                return string.Empty;

            if (s.Contains('"'))
                s = s.Replace("\"", "\"\"");

            if (s.Contains(separador)
                || s.Contains('\n')
                || s.Contains('\r')
                || s.Contains('"'))
            {
                return $"\"{s}\"";
            }

            return s;
        }

        public byte[] GerarExcelBytes(
    Dictionary<string, SheetMapping> mapeamentosPorAba,
    string nomeArquivo)
        {
            using var package = new ExcelPackage();

            foreach (var item in mapeamentosPorAba)
            {
                var aba = item.Value;
                if (aba == null)
                    continue;

                if (aba.Registros == null || !aba.Registros.Any())
                    continue;

                var nomeAba = string.IsNullOrWhiteSpace(aba.NomeAbaB) ? "Planilha" : aba.NomeAbaB;

                // Excel limita em 31 chars
                if (nomeAba.Length > 31)
                    nomeAba = nomeAba.Substring(0, 31);

                var ws = package.Workbook.Worksheets.Add(nomeAba);

                //------------------------------------------------
                // HEADERS
                //------------------------------------------------
                var headers = aba.HeadersA?.Distinct().ToList() ?? new List<string>();

                for (int c = 0; c < headers.Count; c++)
                {
                    ws.Cells[1, c + 1].Value = headers[c];

                    ws.Cells[1, c + 1].Style.Font.Bold = true;
                }

                //------------------------------------------------
                // LINHAS
                //------------------------------------------------
                for (int r = 0; r < aba.Registros.Count; r++)
                {
                    var row = aba.Registros[r];

                    for (int c = 0; c < headers.Count; c++)
                    {
                        var coluna = headers[c];

                        row.Campos.TryGetValue(
                            coluna,
                            out var valor);

                        ws.Cells[r + 2, c + 1].Value =  valor ?? "";
                    }
                }
                ws.Cells.AutoFitColumns();
            }
            //------------------------------------------------
            // FALLBACK
            //------------------------------------------------
            if (package.Workbook.Worksheets.Count == 0)
            {
                var ws = package.Workbook.Worksheets.Add("Vazio");

                ws.Cells[1, 1].Value = "Nenhum dado encontrado";
            }

            return package.GetAsByteArray();
        }

        public async Task<byte[]> GerarExcelBytesMultiSheetAsync(
                        byte[] originalWorkbookBytes,
                        Dictionary<string, SheetMapping> sheets)
        {
            if (sheets == null || !sheets.Any())
                return Array.Empty<byte>();

            using var outPackage = new ExcelPackage();

            foreach (var item in sheets)
            {
                var nomeAba = item.Key;
                var dadosAba = item.Value;

                if (dadosAba == null)
                    continue;

                var registros = dadosAba.Registros;

                if (registros == null || !registros.Any())
                    continue;

                var headersA = dadosAba.HeadersA ?? new List<string>();
                var mapping = dadosAba.Mapping
                              ?? new Dictionary<string, string>();
                var wsOut = outPackage.Workbook.Worksheets.Add(
                    MakeUniqueSheetName(outPackage, nomeAba));

                // 🔹 Cabeçalhos
                for (int c = 0; c < headersA.Count; c++)
                {
                    wsOut.Cells[1, c + 1].Value = headersA[c];
                }

                // 🔹 Dados
                for (int r = 0; r < registros.Count; r++)
                {
                    var row = registros[r];

                    for (int c = 0; c < headersA.Count; c++)
                    {
                        var colunaA = headersA[c];
                        var colunaOrigem = colunaA;

                        if (mapping.TryGetValue(colunaA, out var mapped)
                            && !string.IsNullOrWhiteSpace(mapped))
                        {
                            colunaOrigem = mapped;
                        }

                        row.Campos.TryGetValue(colunaOrigem, out var valor);
                        wsOut.Cells[r + 2, c + 1].Value =
                            valor ?? string.Empty;
                    }
                }
                wsOut.Cells.AutoFitColumns();
            }
            return outPackage.GetAsByteArray();
        }

        private string MakeUniqueSheetName(ExcelPackage pkg, string baseName)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

            var candidate = baseName;

            foreach (var ch in invalidChars)
            {
                candidate = candidate.Replace(ch.ToString(), "");
            }

            candidate = candidate.Trim();

            if (candidate.Length > 31)
                candidate = candidate.Substring(0, 31);

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = "Sheet";

            return candidate;
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
                headers.Add(MapearColuna(Normalize(original)));
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
                headers.Add(MapearColuna(Normalize(original)));
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
            var separador = DetectSeparator(linhas[0]);
            var headers = linhas[0].Split(separador).Select(h => MapearColuna(Normalize(h))).ToArray();

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
