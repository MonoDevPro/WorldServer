using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Loop;

namespace Simulation.Application.Services.ECS;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner : BaseSystem<World, float>, IOrderedUpdatable, IOrderedInitializable
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
    public override void Initialize()
    {
        _systems.Initialize();
    }
    
    public override void BeforeUpdate(in float deltaTime)
    {
        _systems.BeforeUpdate(in deltaTime);
    }

    public override void Update(in float deltaTime)
    {
        _systems.Update(in deltaTime);
    }
    
    public override void AfterUpdate(in float deltaTime)
    {
        _systems.AfterUpdate(in deltaTime);
    }
    
    public override void Dispose()
    {
        _systems.Dispose();
        base.Dispose();
    }

    public int Order { get; } = 3;
    public void Update(float deltaTime)
    {
        BeforeUpdate(in deltaTime);
        Update(in deltaTime);
        AfterUpdate(in deltaTime);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        Dispose();
        return Task.CompletedTask;
    }
}
