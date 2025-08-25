using System.Diagnostics;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Simulation.Core;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner
{
    private readonly SimulationPipeline _systems;

    /// <summary>
    /// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
    /// </summary>
    public SimulationRunner(ILogger<SimulationRunner> logger,
        SimulationPipeline systems)
    {
        _systems = systems;
        
        systems.Configure();
    }

    public void Update(float dt) => Step(dt);

    private void Step(float dt)
    {
        foreach (var system in _systems)
            system.Update(dt);
    }
}
