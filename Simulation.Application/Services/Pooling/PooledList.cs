using System.Collections;
using System.Collections.Concurrent;

namespace Simulation.Application.Services.Pooling;

public sealed class PooledList<T> : IList<T>, IDisposable
{
    private static readonly ConcurrentBag<PooledList<T>> Pool = [];
    private List<T>? _inner;

    private PooledList(int capacity = 0) => _inner = capacity > 0 ? new List<T>(capacity) : [];

    public static PooledList<T> Rent(int initialCapacity = 0)
    {
        if (Pool.TryTake(out var item))
            // optionally ensure capacity
            return item;
        
        return new PooledList<T>(initialCapacity);
    }

    public void Return()
    {
        if (_inner == null) return; // já devolvida
        _inner.Clear();
        Pool.Add(this);
        _inner = null;
    }

    public void Dispose() => Return();

    private List<T> Inner => _inner ?? throw new ObjectDisposedException(nameof(PooledList<T>));

    public T this[int index] { get => Inner[index]; set => Inner[index] = value; }
    public int Count => Inner.Count;
    public bool IsReadOnly => ((ICollection<T>)Inner).IsReadOnly;
    public void Add(T item) => Inner.Add(item);
    public void Clear() => Inner.Clear();
    public bool Contains(T item) => Inner.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => Inner.GetEnumerator();
    public int IndexOf(T item) => Inner.IndexOf(item);
    public void Insert(int index, T item) => Inner.Insert(index, item);
    public bool Remove(T item) => Inner.Remove(item);
    public void RemoveAt(int index) => Inner.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inner).GetEnumerator();

    // utilitário
    public T[] ToArrayAndReturn()
    {
        var arr = Inner.ToArray();
        Return();
        return arr;
    }
}