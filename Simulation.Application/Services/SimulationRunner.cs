using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.Services;

namespace Simulation.Core;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner : BaseSystem<World, float>
{
    private readonly SimulationPipeline _systems;

    /// <summary>
    /// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
    /// </summary>
    public SimulationRunner(ILogger<SimulationRunner> logger,
        World world,
        SimulationPipeline systems) :base(world)
    {
        _systems = systems;
    }

    public override void Update(in float deltaTime)
    {
        foreach (var system in _systems)
            system.Update(deltaTime);
    }
}
