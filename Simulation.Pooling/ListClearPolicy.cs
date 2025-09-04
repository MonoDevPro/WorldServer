using Microsoft.Extensions.ObjectPool;

namespace Simulation.Pooling;

/// <summary>
/// Uma política de pool genérica para qualquer objeto List<T>.
/// A responsabilidade desta política é limpar a lista (`Clear()`) quando ela
/// é devolvida ao pool, garantindo que ela esteja vazia para o próximo uso.
/// </summary>
public class ListClearPolicy<T> : IPooledObjectPolicy<List<T>>
{
    public List<T> Create() => new();

    public bool Return(List<T> list)
    {
        list.Clear();
        return true;
    }
}
