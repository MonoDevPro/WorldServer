using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly ILogger<GameLoop> _logger;

    private readonly IOrderedInitializable[] _initializables;
    private readonly IOrderedUpdatable[] _updatables;
    private readonly PerformanceMonitor? _performanceMonitor;

    // ... (Delegates de logging permanecem os mesmos, estão excelentes) ...
    private static readonly Action<ILogger, Exception?> LogSimulationUpdateError =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogSimulationUpdateError)), "Error during SimulationRunner.Update.");
    private static readonly Action<ILogger, double, double, Exception?> LogTickTooLong =
        LoggerMessage.Define<double, double>(LogLevel.Warning,
            new EventId(3, nameof(LogTickTooLong)),
            "Simulation tick took longer than tick interval: {ElapsedMs} ms (tick {TickMs} ms).");

    public GameLoop(
        ILogger<GameLoop> logger,
        IOptions<LoopOptions> options, // Injeta as opções de configuração
        IEnumerable<IInitializable> allInitializables, // Recebe todos, depois filtra e ordena
        IEnumerable<IUpdatable> allUpdatables, // Recebe todos, depois filtra e ordena
        PerformanceMonitor? performanceMonitor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var loopOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _tickSeconds = 1.0 / loopOptions.TicksPerSecond;
        _maxDeltaTime = loopOptions.MaxDeltaTime;

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

        _logger.LogInformation("Game Loop configured for {TicksPerSecond} TPS ({TickSeconds:F4}s per tick)",
            loopOptions.TicksPerSecond, _tickSeconds);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting initialization for {Count} ordered services...", _initializables.Length);
        foreach (var init in _initializables)
        {
            try
            {
                _logger.LogDebug("Initializing {TypeName} (Order: {Order})...", init.GetType().Name, init.Order);
                await init.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initialization of {TypeName}", init.GetType().Name);
                throw;
            }
        }
        _logger.LogInformation("Initialization complete.");

        _logger.LogInformation("Entering main simulation loop...");
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
                        LogSimulationUpdateError(_logger, ex);
                    }
                    finally
                    {
                        sw.Stop();
                        var tickDurationMs = sw.Elapsed.TotalMilliseconds;
                        _performanceMonitor?.RecordTick(tickDurationMs);

                        if (tickDurationMs > _tickSeconds * 1000)
                        {
                            LogTickTooLong(_logger, tickDurationMs, _tickSeconds * 1000, null);
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
            _logger.LogInformation("RunAsync canceled via token.");
            throw; // Propaga a exceção para que o Host saiba que foi um cancelamento normal.
        }
        finally
        {
            _mainTimer.Stop();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Shutting down server loop...");

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
                _logger.LogWarning(ex, "Error while stopping service {TypeName}", service.GetType().Name);
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
                _logger.LogWarning(ex, "Error while disposing service {TypeName}", service.GetType().Name);
            }
        }

        _performanceMonitor?.Dispose();
        _mainTimer.Stop();
        _logger.LogInformation("Server shutdown complete.");
    }
}
