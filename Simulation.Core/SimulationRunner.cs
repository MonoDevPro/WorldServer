using System.Diagnostics;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Simulation.Core;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner(
    ILogger<SimulationRunner> logger,
    SimulationPipeline systems)
    : BackgroundService
{
    // 20 ticks por segundo (50ms)
    private const double TickSeconds = 1.0 / 20.0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation started");
        var sw = new Stopwatch();
        sw.Start();
        double accumulator = 0;
        var last = sw.Elapsed.TotalSeconds;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = sw.Elapsed.TotalSeconds;
            var frame = now - last;
            last = now;
            accumulator += frame;

            while (accumulator >= TickSeconds)
            {
                Step((float)TickSeconds);
                accumulator -= TickSeconds;
            }

            // Dorme um pouquinho para não ocupar 100% da CPU
            var sleep = Math.Max(0, TickSeconds - accumulator);
            var delayMs = (int)(sleep * 1000.0 / 2); // meio tick de folga
            if (delayMs > 0)
                await Task.Delay(delayMs, stoppingToken).ConfigureAwait(false);
        }
    }

    private void Step(float dt)
    {
        foreach (var system in systems)
            system.Update(dt);
    }
}
