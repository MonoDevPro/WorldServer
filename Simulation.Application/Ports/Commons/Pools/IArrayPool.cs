namespace Simulation.Application.Ports.Commons.Pools;

/// <summary>
/// Define um contrato para um pool de arrays.
/// </summary>
public interface IArrayPool<T>
{
    /// <summary>
    /// Aluga um array do pool com pelo menos o tamanho mínimo especificado.
    /// O array retornado PODE ser maior que o solicitado.
    /// </summary>
    /// <param name="minimumLength">O tamanho mínimo requerido para o array.</param>
    /// <returns>Um array do tipo T.</returns>
    T[] Rent(int minimumLength);

    /// <summary>
    /// Devolve um array ao pool para que possa ser reutilizado.
    /// </summary>
    /// <param name="array">O array a ser devolvido. Não deve ser usado após esta chamada.</param>
    void Return(T[] array);
}
