using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Map;
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
                    _logger.LogError(ex, "Exception while polling network events.");
                    // Continue: normalmente quer manter loop vivo mesmo se rede falhar brevemente
                }

                // --- Simulação (fixed ticks) ---
                var sw = Stopwatch.StartNew();
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
                        _logger.LogError(ex, "Error during SimulationRunner.Update.");
                        // decide se quer abortar totalmente (throw) ou apenas logar e continuar.
                    }
                    finally
                    {
                        sw.Stop();
                        var tickDurationMs = sw.Elapsed.TotalMilliseconds;
                        
                        // Relatório de performance
                        _performanceMonitor?.RecordTick(tickDurationMs);
                        
                        if (tickDurationMs > TickSeconds * 1000)
                        {
                            _logger.LogWarning("Simulation tick took longer than tick interval: {ElapsedMs} ms (tick {TickMs} ms).",
                                tickDurationMs, TickSeconds * 1000);
                        }
                    }

                    accumulator -= TickSeconds;
                }

                // --- Sleep / yield strategy ---
                await SleepUntilNextTick(accumulator, cancellationToken).ConfigureAwait(false);
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
        var remainingTime = TickSeconds - accumulator;
        if (remainingTime <= 0)
        {
            // Estamos atrasados — devolve uma tarefa "rápida" que cede o thread.
            // Usamos Task.Delay(0, ct) para forçar um yield assíncrono sem criar state machine aqui.
            return Task.Delay(0, cancellationToken);
        }

        var delayMilliseconds = (int)(remainingTime * 1000);
        if (delayMilliseconds > 1)
        {
            // Retorna diretamente o Task da API — sem await/async aqui para evitar criar state machine.
            return Task.Delay(delayMilliseconds - 1, cancellationToken);
        }

        // Espera muito curta — usar um delay 0 para ceder.
        return Task.Delay(0, cancellationToken);
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