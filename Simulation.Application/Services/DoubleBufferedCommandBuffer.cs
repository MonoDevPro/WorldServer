using Arch.Buffer;
using Arch.Core;

namespace Simulation.Application.Services;

public sealed class DoubleBufferedCommandBuffer : IDisposable
{
    private readonly CommandBuffer[] _buffers;
    private readonly int[] _writerCounts = new int[2];
    private int _activeIndex;
    private bool _disposed;

    public DoubleBufferedCommandBuffer(int initialCapacity = 128)
    {
        _buffers = new[]
        {
            new CommandBuffer(initialCapacity),
            new CommandBuffer(initialCapacity)
        };
        _activeIndex = 0;
        _writerCounts[0] = 0;
        _writerCounts[1] = 0;
    }

    private int ActiveIndex => Volatile.Read(ref _activeIndex);

    // --- Implementações sem lambda, sem captura de 'in' params ---
    public Entity Create(ComponentType[] types)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));
        var idx = ActiveIndex;
        Interlocked.Increment(ref _writerCounts[idx]);
        try
        {
            return _buffers[idx].Create(types);
        }
        finally
        {
            Interlocked.Decrement(ref _writerCounts[idx]);
        }
    }

    public void Destroy(in Entity entity)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));
        var idx = ActiveIndex;
        Interlocked.Increment(ref _writerCounts[idx]);
        try
        {
            _buffers[idx].Destroy(in entity);
        }
        finally
        {
            Interlocked.Decrement(ref _writerCounts[idx]);
        }
    }

    // Note: aqui mantemos 'in' na assinatura para evitar cópia excessiva de structs grandes,
    // mas não usamos lambda, então não há erro.
    public void Set<T>(in Entity entity, in T? component = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));
        var idx = ActiveIndex;
        Interlocked.Increment(ref _writerCounts[idx]);
        try
        {
            _buffers[idx].Set(in entity, in component);
        }
        finally
        {
            Interlocked.Decrement(ref _writerCounts[idx]);
        }
    }

    public void Add<T>(in Entity entity, in T? component = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));
        var idx = ActiveIndex;
        Interlocked.Increment(ref _writerCounts[idx]);
        try
        {
            _buffers[idx].Add(in entity, in component);
        }
        finally
        {
            Interlocked.Decrement(ref _writerCounts[idx]);
        }
    }

    public void Remove<T>(in Entity entity)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));
        var idx = ActiveIndex;
        Interlocked.Increment(ref _writerCounts[idx]);
        try
        {
            _buffers[idx].Remove<T>(in entity);
        }
        finally
        {
            Interlocked.Decrement(ref _writerCounts[idx]);
        }
    }

    // Swap e playback inalterados (mantive a estratégia SpinWait que você já tinha)
    public void SwapAndPlayback(World world, bool disposeAfterPlayback = true)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedCommandBuffer));

        var prev = Interlocked.Exchange(ref _activeIndex, 1 - _activeIndex);
        var bufferToProcess = _buffers[prev];

        var sw = new SpinWait();
        while (Volatile.Read(ref _writerCounts[prev]) != 0)
            sw.SpinOnce();

        bufferToProcess.Playback(world, dispose: disposeAfterPlayback);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var sw = new SpinWait();
        while (Volatile.Read(ref _writerCounts[0]) != 0 || Volatile.Read(ref _writerCounts[1]) != 0)
            sw.SpinOnce();

        _buffers[0].Dispose();
        _buffers[1].Dispose();
    }
}