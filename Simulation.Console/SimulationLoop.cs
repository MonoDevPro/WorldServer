using System.Diagnostics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Simulation.Core;
using Simulation.Core.Abstractions.Commons.Components.Map;
using Simulation.Network;

namespace Simulation.Console;

public class SimulationLoop : IAsyncDisposable
{
    private readonly ILogger<SimulationLoop> _logger;
    private readonly SimulationRunner _runner;
    private readonly NetworkSystem _network;
    private readonly World _world;

    // 60 ticks por segundo (16.666...ms)
    private const double TickSeconds = 1.0 / 60.0;
    private readonly Stopwatch _mainTimer = new();

    // configuração de sleep (tuning)
    private const int MinDelayMsForTaskDelay = 2; // se >= 2ms, usamos Task.Delay; se < 2ms, usamos Task.Yield

    public SimulationLoop(ILogger<SimulationLoop> logger,
                          SimulationRunner runner,
                          NetworkSystem network,
                          World world)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <summary>
    /// Start the simulation loop. Observes cancellationToken to stop.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulation starting");
        // Start stopwatch before any timing calculations
        _mainTimer.Start();
        double accumulator = 0;
        var last = _mainTimer.Elapsed.TotalSeconds;

        // Enfileira comando para carregar mapa 1 (seed)
        _world.Create(new WantsToLoadMap { MapId = 1 });
        _logger.LogInformation("Comando para carregar mapa 1 enfileirado.");

        // Inicia a network (sincronamente aqui) — se preferir, exponha StartAsync com timeout.
        try
        {
            // Se a sua network.Start pode bloquear por muito tempo, considere rodar em Task.Run com timeout.
            if (!_network.Start())
            {
                _logger.LogCritical("Falha ao iniciar network. Abortando startup do Worker.");
                throw new InvalidOperationException("Falha ao iniciar network");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Exceção ao iniciar o network no StartAsync.");
            throw;
        }

        try
        {
            _logger.LogInformation("Entering main loop.");
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = _mainTimer.Elapsed.TotalSeconds;
                var frame = now - last;
                last = now;

                // cap to avoid pathological large frame deltas
                if (frame > 1.0) frame = 1.0;

                accumulator += frame;

                // Fixed-step updates
                while (accumulator >= TickSeconds && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _runner.Update((float)TickSeconds);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // Shut down requested - break loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro no SimulationRunner.Update()");
                        // continue — não queremos matar o loop apenas por uma exceção
                    }

                    accumulator -= TickSeconds;
                }

                // --- sleeping strategy ---
                var remaining = TickSeconds - accumulator;
                if (remaining <= 0)
                {
                    // we're behind or exactly at tick boundary - yield to avoid busy spin
                    await Task.Yield();
                    continue;
                }

                var remainingMs = remaining * 1000.0;
                if (remainingMs >= MinDelayMsForTaskDelay)
                {
                    // Subtraimos 1ms de margem para aumentar chance de acertar o tick depois do delay
                    var delay = Math.Max(1, (int)remainingMs - 1);
                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException) { /* shutdown */ }
                }
                else
                {
                    // Very short remaining time: use yield (or SpinWait if you need sub-ms precision)
                    await Task.Yield();
                }
            }
        }
        finally
        {
            _logger.LogInformation("Simulation loop ending; stopping network...");
            try
            {
                _network.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao parar Network ao finalizar Worker");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Safe dispose of runner, network, world (if non-null and disposable)
        await SafeDisposeAsync(_runner).ConfigureAwait(false);
        await SafeDisposeAsync(_network).ConfigureAwait(false);
        await SafeDisposeAsync(_world).ConfigureAwait(false);
    }

    private static async ValueTask SafeDisposeAsync(object? resource)
    {
        if (resource == null) return;

        if (resource is IAsyncDisposable asyncDisposable)
        {
            try { await asyncDisposable.DisposeAsync().ConfigureAwait(false); }
            catch { /* swallow or rethrow depending on policy */ }
            return;
        }

        if (resource is IDisposable disposable)
        {
            try { disposable.Dispose(); }
            catch { /* swallow or rethrow depending on policy */ }
        }
    }
}
