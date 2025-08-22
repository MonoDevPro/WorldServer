using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Adapters.In;
using Simulation.Core.Adapters.Out;
using Simulation.Core.Factories;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core;

public static class Services
{
    public static IServiceCollection AddSimulationCore(this IServiceCollection services)
    {
        // Registering the snapshot events service -> 
        services.AddSingleton<SnapshotEvents>();
        services.AddSingleton<ISnapshotEvents>(provider => provider.GetRequiredService<SnapshotEvents>());
        
        // Registering the intent producer service -> Exit from ecs world
        services.AddSingleton<IntentsEnqueueSystem>();
        services.AddSingleton<IIntentProducer, IntentsEnqueueSystem>(provider => provider.GetRequiredService<IntentsEnqueueSystem>());
        
        // World and indices
        services.AddSingleton<IWorldFactory, WorldFactory>();
        services.AddSingleton(provider =>
        {
            var worldFactory = provider.GetRequiredService<IWorldFactory>();
            return worldFactory.Create();
        });
        services.AddSingleton<BlockingIndex>();
        services.AddSingleton<BoundsIndex>();
        services.AddSingleton<SpatialHashGrid>();
        services.AddSingleton<IEntityIndex, EntityIndex>();

        // Systems
        services.AddSingleton<SpawnDespawnSystem>();
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<IndexUpdateSystem>();
        services.AddSingleton<AttackSystem>();
        services.AddSingleton<IntentsDequeueSystem>();
        services.AddSingleton<SnapshotPostSystem>();
        
        // Pipeline
        services.AddSingleton<SimulationPipeline>();
        
        // Simulation runner
        services.AddHostedService<SimulationRunner>();

        return services;
    }
}