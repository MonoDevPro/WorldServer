using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.DTOs;
using Simulation.Application.Options;
using Simulation.Application.Ports.ECS;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.ECS.Utils;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Application.Ports.Loop;
using Simulation.Application.Ports.Persistence;
using Simulation.Application.Services.ECS;
using Simulation.Application.Services.ECS.Handlers;
using Simulation.Application.Services.ECS.Publishers;
using Simulation.Application.Services.ECS.Systems;
using Simulation.Application.Services.ECS.Utils;
using Simulation.Application.Services.ECS.Utils.Factories;
using Simulation.Application.Services.ECS.Utils.Indexers;
using Simulation.Application.Services.Persistence;
using Simulation.Domain.Templates;

namespace Simulation.Application;

public static class ServiceCollection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Default Persistence
        services.AddSingleton<InMemoryMapRepository>();
        services.AddSingleton<InMemoryPlayerRepository>();
        services.AddSingleton<IMapRepository>(sp => sp.GetRequiredService<InMemoryMapRepository>());
        services.AddSingleton<IPlayerRepository>(sp => sp.GetRequiredService<InMemoryPlayerRepository>());
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<InMemoryMapRepository>());
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<InMemoryPlayerRepository>());

        // Get entity providers
        services.AddSingleton<IMapEntityProvider, MapEntityProvider>();
        // Generate snapshots of entities
        services.AddSingleton<IStateSnapshotBuilder, StateSnapshotBuilder>();
        services.AddSingleton<IMapIndex, MapIndex>();
        services.AddSingleton<IPlayerIndex, PlayerIndex>();
        services.AddSingleton<IFactoryHelper<PlayerState>, PlayerFactoryHelper>();
        services.AddSingleton<IFactoryHelper<MapTemplate>, MapFactoryHelper>();
        services.AddSingleton<IMapServiceIndex, SpatialMapIndex>();
        
        services.AddSingleton<World>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorldOptions>>().Value;
            return World.Create(
                chunkSizeInBytes: options.ChunkSizeInBytes, 
                minimumAmountOfEntitiesPerChunk: options.MinimumAmountOfEntitiesPerChunk, 
                archetypeCapacity: options.ArchetypeCapacity, 
                entityCapacity: options.EntityCapacity);
        });
        
        services.AddSingleton<IntentForwarding>();
        services.AddSingleton<IPlayerIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        services.AddSingleton<IMapIntentHandler>(sp => sp.GetRequiredService<IntentForwarding>());
        
        // --- Sistemas da Simulação ---
        // A ordem de registro deve ser respeitada, pois define a ordem de execução no pipeline.
        services.AddSingleton<ISystem<float>>( sp => sp.GetRequiredService<IntentForwarding>());
        services.AddSingleton<ISystem<float>, MapLifecycleSystem>();
        services.AddSingleton<ISystem<float>, PlayerLifecycleSystem>();
        services.AddSingleton<ISystem<float>, GridMovementSystem>();
        services.AddSingleton<ISystem<float>, TeleportSystem>();
        services.AddSingleton<ISystem<float>, AttackSystem>();
        services.AddSingleton<ISystem<float>, LifetimeSystem>();
        services.AddSingleton<ISystem<float>, SpatialIndexSyncSystem>();
        services.AddSingleton<ISystem<float>, SnapshotForwarding>();

        // --- Pipeline e Runner ---
        // O SimulationPipeline irá injetar todos os sistemas registrados acima na ordem correta.
        services.AddSingleton<SimulationPipeline>();
        
        services.AddGameService<SimulationRunner>();
        
        return services;
    }
    
    public static IServiceCollection AddGameService<T>(this IServiceCollection services) 
        where T : class, IInitializable, IUpdatable
    {
        services.AddSingleton<T>();
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<T>());
        services.AddSingleton<IUpdatable>(sp => sp.GetRequiredService<T>());
        return services;
    }
}