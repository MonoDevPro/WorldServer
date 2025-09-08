using System.Collections.Concurrent;
using Simulation.Application.Ports.Pool;

namespace Simulation.Application.Services.Pooling;

/// <summary>
/// Pool genérico simples, thread-safe, com factory e reset action.
/// </summary>
public sealed class DefaultObjectPool<T> : IPool<T> where T : class
{
    private readonly ConcurrentBag<T> _bag = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxRetained; // 0 = unlimited (prático), >0 limit

    public DefaultObjectPool(Func<T> factory, Action<T>? reset = null, int maxRetained = 0)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;
        _maxRetained = maxRetained;
    }

    public T Rent()
    {
        if (_bag.TryTake(out var item))
            return item;
        return _factory();
    }

    public Pooled<T> RentDisposable()
    {
        var item = Rent();
        return new Pooled<T>(this, item);
    }

    public void Return(T item)
    {
        try
        {
            _reset?.Invoke(item);
        }
        catch
        {
            // se reset falhar, melhor não retornar objeto corrompido
            return;
        }

        if (_maxRetained <= 0)
        {
            _bag.Add(item);
            return;
        }

        // se houver limite, tenta manter até o máximo
        if (_bag.Count < _maxRetained)
            _bag.Add(item);
        // senão descarta
    }
}