using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Abstractions.Out;
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
        services.AddSingleton<IIntentProducer, IntentsEnqueueSystem>(provider => provider.GetRequiredService<IntentsEnqueueSystem>());
        
        // World and indices
        services.AddSingleton<IWorldFactory, WorldFactory>();
        services.AddSingleton(provider =>
        {
            var worldFactory = provider.GetRequiredService<IWorldFactory>();
            return worldFactory.Create();
        });
        services.AddSingleton<ISpatialIndex, SpatialHashGrid>();
        services.AddSingleton<IEntityIndex, EntityIndex>();

        // Systems
        services.AddSingleton<MapLoaderSystem>();
        services.AddSingleton<IntentsEnqueueSystem>();
        services.AddSingleton<IntentsDequeueSystem>();
        services.AddSingleton<SpawnDespawnSystem>();
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<SpatialIndexCommitSystem>();
        services.AddSingleton<AttackSystem>();
        services.AddSingleton<SnapshotPostSystem>();
        
        // Pipeline
        services.AddSingleton<SimulationPipeline>();
        
        // Simulation runner
        services.AddHostedService<SimulationRunner>();

        return services;
    }
}