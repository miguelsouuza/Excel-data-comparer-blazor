using System.Text.RegularExpressions;

public class AutoMappingService : IAutoMappingService
{
    public Dictionary<string, string> GerarMapeamento(
        List<string> colunasA,
        List<string> colunasB)
    {
        var resultado = new Dictionary<string, string>();

        foreach (var colA in colunasA)
        {
            var melhor = colunasB
                .Select(colB => new
                {
                    Coluna = colB,
                    Score = Similaridade(colA, colB)
                })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor != null && melhor.Score > 0.5)
                resultado[colA] = melhor.Coluna;
        }

        return resultado;
    }

    // Alinha a Base B para ter as mesmas colunas e ordem da Base A.
    public (List<GenericRegistration> Alinhada, Dictionary<string, string> Mapeamento) AlinharBaseB(
        List<GenericRegistration> baseA,
        List<GenericRegistration> baseB)
    {
        var headersA = baseA?.FirstOrDefault()?.Campos.Keys.ToList() ?? new List<string>();
        var headersB = baseB?.FirstOrDefault()?.Campos.Keys.ToList() ?? new List<string>();

        var setA = headersA.Select(h => h).ToHashSet();
        var setB = headersB.Select(h => h).ToHashSet();

        var apenasA = setA.Except(setB).ToList();
        var apenasB = setB.Except(setA).ToList();

        var mapeamento = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1) mantenha colunas que já existem com mesmo nome
        foreach (var h in headersA)
        {
            if (headersB.Contains(h))
                mapeamento[h] = h;
        }

        // 2) tentar mapear apenasA <-> apenasB por padrão de dados
        foreach (var colA in apenasA)
        {
            string? bestMatch = null;
            double bestScore = 0;

            foreach (var colB in apenasB)
            {
                var score = CompareColumnPattern(baseA, colA, baseB, colB);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = colB;
                }
            }

            // se alta similaridade assume mapeamento
            if (bestMatch != null && bestScore >= 0.8)
            {
                mapeamento[colA] = bestMatch;
            }
        }

        // 3) construir lista de colunas finais para Base B (remover extras vazios ou não mapeados)
        var mappedValues = mapeamento.Values.Where(v => !string.IsNullOrWhiteSpace(v)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // detectar colunas em B que são totalmente vazias
        var emptyColsB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in headersB)
        {
            bool allEmpty = true;
            foreach (var row in baseB)
            {
                if (row.Campos.TryGetValue(col, out var v) && !string.IsNullOrWhiteSpace(v))
                {
                    allEmpty = false; break;
                }
            }
            if (allEmpty) emptyColsB.Add(col);
        }

        // columns to keep in B: any that are mapped (mappedValues) or that exist in A (handled above)
        var keepInB = new HashSet<string>(mappedValues, StringComparer.OrdinalIgnoreCase);
        foreach (var h in headersB)
        {
            if (headersA.Contains(h)) keepInB.Add(h);
        }

        // remove leftover columns that are neither mapped nor present in A and are empty
        var removable = headersB.Where(h => !keepInB.Contains(h) && emptyColsB.Contains(h)).ToList();

        // -> Adicionar colunas mapeadas diretamente na baseB (mesmo padrão)
        // Para cada coluna A mapeada para uma coluna B, criaremos a coluna A em cada registro de baseB
        // copiando os valores da coluna B. Para colunas A sem mapeamento, garantimos a presença da chave com valor vazio.
        foreach (var colA in headersA)
        {
            if (mapeamento.TryGetValue(colA, out var srcCol) && !string.IsNullOrWhiteSpace(srcCol))
            {
                foreach (var row in baseB)
                {
                    if (row.Campos.TryGetValue(srcCol, out var v))
                        row.Campos[colA] = v ?? "";
                    else
                        row.Campos[colA] = "";
                }
            }
            else
            {
                // não mapeado: apenas garantir a coluna existe (vazia)
                foreach (var row in baseB)
                {
                    if (!row.Campos.ContainsKey(colA))
                        row.Campos[colA] = "";
                }
            }
        }

        // Remover colunas detectadas como "removable" diretamente da baseB para manter consistência
        if (removable.Any())
        {
            foreach (var row in baseB)
            {
                foreach (var rem in removable)
                {
                    if (row.Campos.ContainsKey(rem))
                        row.Campos.Remove(rem);
                }
            }
        }

        // 4) criar nova Base B alinhada usando a ordem de headersA
        var aligned = new List<GenericRegistration>();
        foreach (var row in baseB)
        {
            var novo = new GenericRegistration();

            foreach (var colA in headersA)
            {
                // obter coluna correspondente em B
                if (mapeamento.TryGetValue(colA, out var colB) && !string.IsNullOrWhiteSpace(colB))
                {
                    row.Campos.TryGetValue(colB, out var varB);
                    novo.Campos[colA] = varB ?? "";
                }
                else
                {
                    // se não mapeado, tenta pegar mesmo nome
                    row.Campos.TryGetValue(colA, out var varB);
                    novo.Campos[colA] = varB ?? "";
                }
            }

            aligned.Add(novo);
        }

        // 5) ajustar mapeamento final para incluir identity mappings for existing columns
        foreach (var h in headersA)
        {
            if (!mapeamento.ContainsKey(h))
            {
                // se B tinha a mesma coluna, já garantido antes, senão mapeado para empty string
                if (headersB.Contains(h)) mapeamento[h] = h;
                else mapeamento[h] = "";
            }
        }

        return (aligned, mapeamento);
    }

    private double Similaridade(string a, string b)
    {
        a = Normalizar(a);
        b = Normalizar(b);

        if (a == b) return 1;

        if (a.Contains(b) || b.Contains(a)) return 0.8;

        var pa = a.Split('_');
        var pb = b.Split('_');

        var inter = pa.Intersect(pb).Count();
        var total = pa.Union(pb).Count();

        return total == 0 ? 0 : (double)inter / total;
    }

    private string Normalizar(string t)
    {
        return Regex.Replace(t.ToUpper(), @"[^A-Z0-9_]", "");
    }

    // Retorna um score (0..1) indicando similaridade entre padrões de dados nas duas colunas
    private double CompareColumnPattern(
        List<GenericRegistration> baseA, string colA,
        List<GenericRegistration> baseB, string colB)
    {
        if (baseA == null || baseB == null) return 0;

        var samplesA = baseA.Select(r => r.Campos.ContainsKey(colA) ? r.Campos[colA] : "").Where(s => !string.IsNullOrWhiteSpace(s)).Take(200).ToList();
        var samplesB = baseB.Select(r => r.Campos.ContainsKey(colB) ? r.Campos[colB] : "").Where(s => !string.IsNullOrWhiteSpace(s)).Take(200).ToList();

        if (!samplesA.Any() || !samplesB.Any()) return 0;

        double score = 0;

        // tipo predominante: date, integer, decimal, cnpj/cpf, email, alphanumeric
        var typeA = DetectType(samplesA);
        var typeB = DetectType(samplesB);

        if (typeA == typeB) score += 0.7; // peso alto para tipo

        // tamanho médio
        var lenA = samplesA.Average(s => s.Length);
        var lenB = samplesB.Average(s => s.Length);
        var lenDiff = Math.Abs(lenA - lenB) / Math.Max(1, Math.Max(lenA, lenB));
        score += 0.3 * (1 - lenDiff);

        return Math.Max(0, Math.Min(1, score));
    }

    private string DetectType(List<string> samples)
    {
        int date = 0, integer = 0, dec = 0, cnpjcpf = 0, email = 0, other = 0;

        var reCnpjCpf = new Regex(@"\d{11}|\d{14}");
        var reEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        foreach (var s in samples)
        {
            var t = s.Trim();
            if (string.IsNullOrEmpty(t)) continue;

            if (DateTime.TryParse(t, out _)) { date++; continue; }
            if (long.TryParse(t, out _)) { integer++; continue; }
            if (double.TryParse(t, out _)) { dec++; continue; }
            if (reCnpjCpf.IsMatch(Regex.Replace(t, "[^0-9]", ""))) { cnpjcpf++; continue; }
            if (reEmail.IsMatch(t)) { email++; continue; }

            other++;
        }

        var max = new[] { ("date", date), ("int", integer), ("dec", dec), ("cnpjcpf", cnpjcpf), ("email", email), ("other", other) }
            .OrderByDescending(x => x.Item2)
            .First();

        return max.Item1;
    }
}