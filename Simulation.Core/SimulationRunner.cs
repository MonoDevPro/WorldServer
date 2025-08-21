using System.Diagnostics;
using Arch.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Systems;

namespace Simulation.Core;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner : BackgroundService
{
    private readonly ILogger<SimulationRunner> _logger;
    private readonly World _world;
    private readonly ISimulationRequests _requests;
    private readonly GridMovementSystem _gridMovement;
    private readonly TeleportSystem _teleport;
    private readonly IndexUpdateSystem _indexUpdate;
    private readonly AttackSystem _attack;

    // 20 ticks por segundo (50ms)
    private const double TickSeconds = 1.0 / 20.0;

    public SimulationRunner(
        ILogger<SimulationRunner> logger,
        World world,
        ISimulationRequests requests,
        GridMovementSystem gridMovement,
        TeleportSystem teleport,
        IndexUpdateSystem indexUpdate,
        AttackSystem attack)
    {
        _logger = logger;
        _world = world;
        _requests = requests;
        _gridMovement = gridMovement;
        _teleport = teleport;
        _indexUpdate = indexUpdate;
        _attack = attack;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulation started");
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

            // Processa comandos antes do tick
            DrainAndApplyCommands();

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

    private void DrainAndApplyCommands()
    {
        while (_requests.TryDequeueTeleport(out var tp))
            _teleport.Apply(tp);
        while (_requests.TryDequeueMove(out var mv))
            _gridMovement.Apply(mv);
        while (_requests.TryDequeueAttack(out var atk))
            _attack.Apply(atk);
    }

    private void Step(float dt)
    {
        _gridMovement.Update(dt);
        _attack.Update(dt);
        _indexUpdate.Update(dt);
    }
}
