using Microsoft.Extensions.ObjectPool;

namespace Simulation.Pooling;

/// <summary>
/// Uma política de pool genérica para qualquer objeto List<T>.
/// A responsabilidade desta política é limpar a lista (`Clear()`) quando ela
/// é devolvida ao pool, garantindo que ela esteja vazia para o próximo uso.
/// </summary>
public sealed class PooledListPolicy<T>(int maxAllowedCapacity = 1024) : PooledObjectPolicy<List<T>>
{
    private readonly int _maxAllowedCapacity = Math.Max(16, maxAllowedCapacity);

    public override List<T> Create() => new List<T>();

    public override bool Return(List<T> obj)
    {
        // Limpa para evitar retenção de referências
        obj.Clear();

        // Reduz capacity de listas que crescem demais para não reter memória grande
        if (obj.Capacity > _maxAllowedCapacity)
        {
            obj.Capacity = _maxAllowedCapacity;
        }

        return true;
    }
}