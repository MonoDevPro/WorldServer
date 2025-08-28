using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Abstractions.Ports;
using Simulation.Core.Adapters;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;
using Simulation.Core.Utilities.Factories;
using PlayerLifecycleSystem = Simulation.Core.Adapters.PlayerLifecycleSystem;

namespace Simulation.Core;

public static class Services
{
    public static IServiceCollection AddSimulationCore(this IServiceCollection services)
    {
        // World and indices
        services.AddSingleton<World>(provider => WorldFactory.Create());
        services.AddSingleton<ICharIndex, CharIndex>();
        services.AddSingleton<ISpatialIndex, SpatialIndex>();
        services.AddSingleton<IEntityIndex, EntityIndex>();
        services.AddSingleton<IMapIndex, MapIndex>();

        // Systems
        services.AddSingleton<MapLoaderSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<IMapLoaderSystem>(sp => sp.GetRequiredService<MapLoaderSystem>());
        
        // Registering the lifecycle service -> Update and manage entity lifetimes
        services.AddSingleton<PlayerLifecycleSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<PlayerSpawnSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<PlayerDespawnSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<ILifecycleSystem, PlayerLifecycleSystem>(provider => provider.GetRequiredService<PlayerLifecycleSystem>());
        
        // Registering the intent producer service -> Exit from ecs world
        services.AddSingleton<IntentEnqueueSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<IIntentHandler, IntentEnqueueSystem>(provider => provider.GetRequiredService<IntentEnqueueSystem>());
        
        // Registering the snapshot events service -> 
        services.AddSingleton<SnapshotPublisherSystem>(); // BaseSystem — registre e chame Update no loop
        services.AddSingleton<ISnapshotPublisher>(provider => provider.GetRequiredService<SnapshotPublisherSystem>());
        
        services.AddSingleton<IntentsDequeueSystem>();
        services.AddSingleton<LifetimeSystem>();
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<SpatialIndexCommitSystem>();
        services.AddSingleton<AttackSystem>();
        services.AddSingleton<SnapshotPostSystem>();
        
        // Pipeline
        services.AddSingleton<SimulationPipeline>();
        
        // Simulation runner
        services.AddSingleton<SimulationRunner>();

        return services;
    }
}