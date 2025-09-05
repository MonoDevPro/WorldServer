using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Simulation.Application.Services;
using Simulation.Networking;

namespace Simulation.Server;

/// <summary>
/// Orquestra o ciclo de vida do servidor, incluindo inicialização, o loop principal do jogo e o desligamento.
/// </summary>
public class ServerLoop : IAsyncDisposable
{
    private const double TickSeconds = 1.0 / 60.0; // 60 tps
    private readonly Stopwatch _mainTimer = new();
    private readonly ILogger<ServerLoop> _logger;
    private readonly SimulationRunner _simulationRunner;
    private readonly LiteNetServer _networkServer;
    private readonly PerformanceMonitor? _performanceMonitor;

    // Precompiled logging delegates to avoid allocating object[] and boxing on each log call.
    private static readonly Action<ILogger, Exception?> LogPollEventsError =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(LogPollEventsError)), "Exception while polling network events.");

    private static readonly Action<ILogger, Exception?> LogSimulationUpdateError =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogSimulationUpdateError)), "Error during SimulationRunner.Update.");

    private static readonly Action<ILogger, double, double, Exception?> LogTickTooLong =
        LoggerMessage.Define<double, double>(LogLevel.Warning,
            new EventId(3, nameof(LogTickTooLong)),
            "Simulation tick took longer than tick interval: {ElapsedMs} ms (tick {TickMs} ms).");

    public ServerLoop(
        ILogger<ServerLoop> logger,
        SimulationRunner simulationRunner,
        LiteNetServer networkServer,
        PerformanceMonitor? performanceMonitor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _simulationRunner = simulationRunner ?? throw new ArgumentNullException(nameof(simulationRunner));
        _networkServer = networkServer ?? throw new ArgumentNullException(nameof(networkServer));
        _performanceMonitor = performanceMonitor;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting network server...");
        _networkServer.Start();

        _logger.LogInformation("Entering main simulation loop...");
        _mainTimer.Start();

        double accumulator = 0.0;
        double lastTime = _mainTimer.Elapsed.TotalSeconds;
        var sw = new Stopwatch(); // reutilizado por tick — evita StartNew() por iteração

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = _mainTimer.Elapsed.TotalSeconds;
                var deltaTime = currentTime - lastTime;
                lastTime = currentTime;

                // clamp para evitar spiral of death
                if (deltaTime > 0.25) deltaTime = 0.25;
                accumulator += deltaTime;

                // --- Rede ---
                try
                {
                    _networkServer.PollEvents();
                }
                catch (Exception ex)
                {
                    // Usa delegate pré-compilado (sem object[] allocation)
                    LogPollEventsError(_logger, ex);
                }

                // --- Simulação (fixed ticks) ---
                while (accumulator >= TickSeconds && !cancellationToken.IsCancellationRequested)
                {
                    sw.Restart();
                    try
                    {
                        _simulationRunner.Update((float)TickSeconds);
                    }
                    catch (OperationCanceledException)
                    {
                        // propagate cancellation silently to exit
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

                        // Relatório de performance (não aloca aqui)
                        _performanceMonitor?.RecordTick(tickDurationMs);

                        if (tickDurationMs > TickSeconds * 1000)
                        {
                            LogTickTooLong(_logger, tickDurationMs, TickSeconds * 1000, null);
                        }
                    }

                    accumulator -= TickSeconds;
                }

                // --- Sleep / yield strategy ---
                if (accumulator <= 0)
                {
                    // Estamos atrasados — forçar yield para evitar busy-spin.
                    // Task.Yield() não aloca Task e é a forma adequada de ceder o thread aqui.
                    await Task.Yield();
                }
                else
                {
                    await SleepUntilNextTick(accumulator, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RunAsync canceled via token.");
            throw;
        }
        finally
        {
            _mainTimer.Stop();
        }
    }

    private Task SleepUntilNextTick(double accumulator, CancellationToken cancellationToken)
    {
        // remainingTime só é usado quando chegamos aqui com accumulator > 0 e accumulator < TickSeconds.
        var remainingTime = TickSeconds - accumulator;
        if (remainingTime <= 0)
        {
            // Caso raro: não precisamos dormir; devolvemos CompletedTask (sem alocação).
            return Task.CompletedTask;
        }

        var delayMilliseconds = (int)(remainingTime * 1000);
        if (delayMilliseconds > 1)
        {
            // Retorna diretamente o Task da API — cria timer apenas quando realmente for dormir.
            return Task.Delay(delayMilliseconds - 1, cancellationToken);
        }

        // Para esperas muito curtas preferimos não criar timer; devolvemos CompletedTask.
        // Observação: o chamador pode escolher fazer Task.Yield() para forçar yield quando necessário.
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogInformation("Shutting down server loop...");

        // Stop network first so no more incoming events during shutdown
        try
        {
            _networkServer.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping network server.");
        }

        // Dispose / DisposeAsync simulation runner
        try
        {
            if (_simulationRunner is IDisposable disposable)
                disposable.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disposing SimulationRunner.");
        }

        // Dispose performance monitor
        try
        {
            _performanceMonitor?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disposing PerformanceMonitor.");
        }

        _mainTimer.Stop();
        _logger.LogInformation("Server shutdown complete.");
        return ValueTask.CompletedTask;
    }
}
