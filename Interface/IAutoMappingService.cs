public interface IAutoMappingService
{
    Dictionary<string, string> GerarMapeamento(
        List<string> colunasA,
        List<string> colunasB);

    // Alinha a Base B para ficar com as mesmas colunas e ordem da Base A.
    // Retorna a tupla com a Base B alinhada e o mapeamento utilizado (chave = coluna A, valor = coluna B original).
    (List<GenericRegistration> Alinhada, Dictionary<string, string> Mapeamento) AlinharBaseB(
        List<GenericRegistration> baseA,
        List<GenericRegistration> baseB);

}
