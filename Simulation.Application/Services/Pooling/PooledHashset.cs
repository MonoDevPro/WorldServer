using System.Collections;
using System.Collections.Concurrent;

namespace Simulation.Application.Services.Pooling;

public sealed class PooledHashSet<T> : IEnumerable<T>, IDisposable
{
    private static readonly ConcurrentBag<PooledHashSet<T>> _pool = new();
    private HashSet<T>? _inner;

    private PooledHashSet(int capacity = 0) => _inner = new HashSet<T>();

    public static PooledHashSet<T> Rent() => _pool.TryTake(out var item) ? item : new PooledHashSet<T>();

    public void Return()
    {
        if (_inner == null) return;
        _inner.Clear();
        _pool.Add(this);
        _inner = null;
    }

    public void Dispose() => Return();

    private HashSet<T> Inner => _inner ?? throw new ObjectDisposedException(nameof(PooledHashSet<T>));

    public bool Add(T item) => Inner.Add(item);
    public bool Remove(T item) => Inner.Remove(item);
    public bool Contains(T item) => Inner.Contains(item);
    public int Count => Inner.Count;
    public void Clear() => Inner.Clear();
    public IEnumerator<T> GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();
}