using Simulation.Application.Ports.Pool;

namespace Simulation.Application.Services.Pooling;

/// <summary>
/// Wrapper que devolve ao pool quando descartado. Após Dispose(), o acesso a Value lança ObjectDisposedException.
/// </summary>
public sealed class Pooled<T> : IDisposable where T : class
{
    private readonly IPool<T>? _pool;
    private T? _value;
    private bool _disposed;

    internal Pooled(IPool<T> pool, T value)
    {
        _pool = pool;
        _value = value;
        _disposed = false;
    }

    public T Value => _disposed ? throw new ObjectDisposedException(nameof(Pooled<T>)) : _value!;

    public void Dispose()
    {
        if (_disposed) return;
        var v = _value;
        _value = null;
        _disposed = true;
        if (v != null)
        {
            try { _pool?.Return(v); }
            catch { /* swallow to avoid exceptions on dispose */ }
        }
    }
}