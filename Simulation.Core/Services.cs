using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Adapters.In;
using Simulation.Core.Adapters.In.Factories;
using Simulation.Core.Adapters.Out;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core;

public static class Services
{
    public static IServiceCollection AddSimulationCore(this IServiceCollection services)
    {
        // Registering the snapshot events service
        services.AddSingleton<ISnapshotEvents, SnapshotEvents>();
    services.AddSingleton<IEntityIndex, EntityIndex>();
        
        // Registering the world factory service
        services.AddSingleton<IWorldFactory, WorldFactory>();
        
        // Registering the map factory service
        services.AddSingleton<IMapFactory, MapFactory>();

        // Registering the entity factory service
        services.AddSingleton<IEntityFactory, EntityFactory>();

        // Request queue port
        services.AddSingleton<ISimulationRequests, SimulationRequests>();

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

        // Systems
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<IndexUpdateSystem>();
        services.AddSingleton<AttackSystem>();

        // Simulation runner
        services.AddHostedService<SimulationRunner>();

        return services;
    }
}