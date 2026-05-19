public interface IAutoMappingService
{
    Dictionary<string, string> GerarMapeamento(
        List<string> colunasA,
        List<string> colunasB);

    // Alinha a Base B para ficar com as mesmas colunas e ordem da Base A.
    // Retorna a tupla com a Base B alinhada e o mapeamento utilizado (chave = coluna A, valor = coluna B original).
    AlignmentResult AlinharBaseB(
     List<GenericRegistration> baseA,
     List<GenericRegistration> baseB,
     Dictionary<string, string>? colunasFixasDaBaseA = null,
     Dictionary<string, string>? valoresFixos = null);

}
