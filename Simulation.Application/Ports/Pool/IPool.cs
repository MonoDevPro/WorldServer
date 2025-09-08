using Simulation.Application.Services.Pooling;

namespace Simulation.Application.Ports.Pool;

public interface IPool<T> where T : class
{
    /// <summary>Obtém uma instância do pool (pode ser recém-criada).</summary>
    T Rent();

    /// <summary>Devolve uma instância ao pool.</summary>
    void Return(T item);

    /// <summary>Rents an item and returns a disposable wrapper that will Return() when disposed.</summary>
    Pooled<T> RentDisposable();
}