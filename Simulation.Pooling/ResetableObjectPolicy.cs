using Simulation.Application.Ports.Commons.Pools;

namespace Simulation.Pooling;

using Microsoft.Extensions.ObjectPool;

/// <summary>
/// Uma política de pool genérica para qualquer objeto que implemente IResetable
/// e tenha um construtor padrão.
/// Você escreve esta classe UMA VEZ e a reutiliza para todos os seus DTOs.
/// </summary>
public class ResetableObjectPolicy<T> : IPooledObjectPolicy<T> where T : class, IResetable, new()
{
    public T Create()
    {
        // Cria uma nova instância usando o construtor padrão (new()).
        return new T();
    }

    public bool Return(T obj)
    {
        // A política simplesmente delega a responsabilidade de resetar
        // para o próprio objeto. Perfeito!
        obj.Reset();
        return true;
    }
}