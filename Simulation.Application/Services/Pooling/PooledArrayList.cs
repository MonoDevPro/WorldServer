using System.Buffers;

namespace Simulation.Application.Services.Pooling;

public sealed class PooledArrayList<T> : IDisposable
{
    private T[]? _buffer;
    public int Count { get; private set; }

    private PooledArrayList(int capacity)
    {
        _buffer = ArrayPool<T>.Shared.Rent(Math.Max(4, capacity));
        Count = 0;
    }

    public static PooledArrayList<T> Rent(int capacity = 16) => new PooledArrayList<T>(capacity);

    public void Add(T item)
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(PooledArrayList<T>));
        if (Count >= _buffer.Length)
        {
            var newBuf = ArrayPool<T>.Shared.Rent(_buffer.Length * 2);
            Array.Copy(_buffer, 0, newBuf, 0, Count);
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = newBuf;
        }
        _buffer[Count++] = item;
    }

    public Span<T> AsSpan()
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(PooledArrayList<T>));
        return new Span<T>(_buffer, 0, Count);
    }

    public void Clear() => Count = 0;

    public void Dispose()
    {
        if (_buffer != null)
        {
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = null;
            Count = 0;
        }
    }

    public T[] ToArrayAndReturn()
    {
        if (_buffer == null) throw new ObjectDisposedException(nameof(PooledArrayList<T>));
        var ret = new T[Count];
        Array.Copy(_buffer, 0, ret, 0, Count);
        Dispose();
        return ret;
    }
}