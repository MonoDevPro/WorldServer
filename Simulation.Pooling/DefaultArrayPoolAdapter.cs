using System.Buffers;
using Simulation.Application.Ports.Commons.Pools;

namespace Simulation.Pooling;

/// <summary>
/// Implementação do nosso contrato IArrayPool<T> que usa a classe System.Buffers.ArrayPool<T>
/// padrão do .NET por baixo dos panos.
/// </summary>
public class DefaultArrayPoolAdapter<T> : IArrayPool<T>
{
    private readonly ArrayPool<T> _dotnetPool = ArrayPool<T>.Shared;

    public T[] Rent(int minimumLength)
    {
        return _dotnetPool.Rent(minimumLength);
    }

    public void Return(T[] array)
    {
        // O clearArray: true é importante para zerar os dados e evitar
        // que informações "vazem" entre diferentes usos do mesmo array.
        _dotnetPool.Return(array, clearArray: true);
    }
}