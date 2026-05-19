using DataComparer.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public class HeaderCompareService : IHeaderCompareService
{
    public HeaderCompareResult Comparar(List<string> colunasA, List<string> colunasB)
    {
        var resultado = new HeaderCompareResult();

        var setA = colunasA.Select(Helpers.Normalize).ToHashSet();
        var setB = colunasB.Select(Helpers.Normalize).ToHashSet();

        resultado.EmComum = setA.Intersect(setB).ToList();
        resultado.ApenasA = setA.Except(setB).ToList();
        resultado.ApenasB = setB.Except(setA).ToList();

        return resultado;
    }

}