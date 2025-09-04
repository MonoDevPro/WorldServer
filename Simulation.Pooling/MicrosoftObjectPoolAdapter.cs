using Microsoft.Extensions.ObjectPool;
using Simulation.Application.Ports.Commons.Pools;

namespace Simulation.Pooling;

/// <summary>
/// Implementação do nosso contrato IObjectPool que usa a biblioteca padrão do .NET por baixo dos panos.
/// Esta é uma classe de "infraestrutura".
/// </summary>
public class MicrosoftObjectPoolAdapter<T>(ObjectPool<T> microsoftPool) : IObjectPool<T>
    where T : class
{
    // A dependência da biblioteca externa fica encapsulada aqui.

    public T Get()
    {
        return microsoftPool.Get();
    }

    public void Return(T obj)
    {
        microsoftPool.Return(obj);
    }
}