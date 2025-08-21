using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Adapters.In;
using Simulation.Core.Adapters.In.Factories;
using Simulation.Core.Adapters.Out;

namespace Simulation.Core;

public static class Services
{
    public static IServiceCollection AddSimulationCore(this IServiceCollection services)
    {
        // Registering the snapshot events service
        services.AddSingleton<ISnapshotEvents, SnapshotEvents>();
        
        // Registering the world factory service
        services.AddSingleton<IWorldFactory, WorldFactory>();
        
        // Registering the map factory service
        services.AddSingleton<IMapFactory, MapFactory>();

        // Registering the simulation service
        services.AddSingleton<ISimulationService, SimulationService>();
        
        // Registering the entity factory service
        services.AddSingleton<IEntityFactory, EntityFactory>();

        return services;
    }
}