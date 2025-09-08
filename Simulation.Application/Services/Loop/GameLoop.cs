using System.Diagnostics;
using Simulation.Application.Options;
using Simulation.Application.Ports.Loop;

namespace Simulation.Application.Services.Loop;

/// <summary>
/// Orquestra o ciclo de vida do servidor, incluindo inicialização, o loop principal do jogo e o desligamento.
/// Garante a ordem de execução dos serviços.
/// </summary>
public sealed class GameLoop : IAsyncDisposable
{
    private readonly double _tickSeconds;
    private readonly double _maxDeltaTime;
    private readonly Stopwatch _mainTimer = new();

    private readonly IOrderedInitializable[] _initializables;
    private readonly IOrderedUpdatable[] _updatables;
    private readonly PerformanceMonitor? _performanceMonitor;

    public GameLoop(
        GameLoopOptions options, // Injeta as opções de configuração
        IEnumerable<IInitializable> allInitializables, // Recebe todos, depois filtra e ordena
        IEnumerable<IUpdatable> allUpdatables, // Recebe todos, depois filtra e ordena
        PerformanceMonitor? performanceMonitor = null)
    {
        _tickSeconds = 1.0 / options.TicksPerSecond;
        _maxDeltaTime = options.MaxDeltaTime;

        // **GARANTIA DE ORDEM**
        // Filtra e ordena os serviços que implementam as interfaces ordenadas.
        _initializables = allInitializables
            .OfType<IOrderedInitializable>()
            .OrderBy(s => s.Order)
            .ToArray();
        
        _updatables = allUpdatables
            .OfType<IOrderedUpdatable>()
            .OrderBy(s => s.Order)
            .ToArray();

        _performanceMonitor = performanceMonitor;

    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var init in _initializables)
        {
            try
            {
                await init.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        _mainTimer.Start();

        double accumulator = 0.0;
        double lastTime = _mainTimer.Elapsed.TotalSeconds;
        var sw = new Stopwatch();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = _mainTimer.Elapsed.TotalSeconds;
                var deltaTime = currentTime - lastTime;
                lastTime = currentTime;

                // clamp para evitar spiral of death (agora configurável)
                if (deltaTime > _maxDeltaTime) deltaTime = _maxDeltaTime;
                accumulator += deltaTime;

                // --- Simulação (fixed ticks) ---
                while (accumulator >= _tickSeconds && !cancellationToken.IsCancellationRequested)
                {
                    sw.Restart();
                    try
                    {
                        for (var i = 0; i < _updatables.Length; i++)
                            _updatables[i].Update((float)_tickSeconds);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        sw.Stop();
                        var tickDurationMs = sw.Elapsed.TotalMilliseconds;
                        _performanceMonitor?.RecordTick(tickDurationMs);

                        if (tickDurationMs > _tickSeconds * 1000)
                        {
                        }
                    }

                    accumulator -= _tickSeconds;
                }

                // --- Sleep / yield strategy ---
                if (accumulator < _tickSeconds)
                {
                    // Dorme pelo tempo restante até o próximo tick
                    var remainingTime = _tickSeconds - accumulator;
                    var delayMs = (int)(remainingTime * 1000);
                    if (delayMs > 1)
                    {
                        await Task.Delay(delayMs -1, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Se o tempo restante for muito curto ou negativo (estamos atrasados),
                        // apenas cedemos o controlo para evitar um loop apertado (busy-wait).
                        await Task.Yield();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Propaga a exceção para que o Host saiba que foi um cancelamento normal.
        }
        finally
        {
            _mainTimer.Stop();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // **ROBUSTEZ MELHORADA**
        // Inverte a ordem para o shutdown (LIFO - Last In, First Out).
        // Envolve cada chamada em try-catch para garantir que todos os serviços tentem parar.
        foreach (var service in _initializables.Reverse())
        {
            try
            {
                await service.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
            }
        }
        
        foreach (var service in _initializables.Reverse())
        {
            try
            {
                await service.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
            }
        }

        _performanceMonitor?.Dispose();
        _mainTimer.Stop();
    }
}
