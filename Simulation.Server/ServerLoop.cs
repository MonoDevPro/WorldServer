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

    public ServerLoop(
        ILogger<ServerLoop> logger,
        SimulationRunner simulationRunner,
        LiteNetServer networkServer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _simulationRunner = simulationRunner ?? throw new ArgumentNullException(nameof(simulationRunner));
        _networkServer = networkServer ?? throw new ArgumentNullException(nameof(networkServer));
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
                while (accumulator >= TickSeconds && !cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();
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
                        if (sw.Elapsed.TotalSeconds > TickSeconds)
                        {
                            _logger.LogWarning("Simulation tick took longer than tick interval: {ElapsedMs} ms (tick {TickMs} ms).",
                                sw.Elapsed.TotalMilliseconds, TickSeconds * 1000);
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

    private async Task SleepUntilNextTick(double accumulator, CancellationToken cancellationToken)
    {
        var remainingTime = TickSeconds - accumulator;
        if (remainingTime <= 0)
        {
            await Task.Yield(); // Estamos atrasados, cede o controle do thread brevemente.
            return;
        }

        var delayMilliseconds = (int)(remainingTime * 1000);
        if (delayMilliseconds > 1)
        {
            try
            {
                await Task.Delay(delayMilliseconds - 1, cancellationToken);
            }
            catch (TaskCanceledException) { /* Ignora, o cancelamento será tratado no loop principal. */ }
        }
        else
        {
            await Task.Yield(); // Para esperas muito curtas, Task.Yield é mais apropriado.
        }
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

        _mainTimer.Stop();
        _logger.LogInformation("Server shutdown complete.");
        return ValueTask.CompletedTask;
    }
}