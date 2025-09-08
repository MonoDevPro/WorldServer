using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Options;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.ECS.Services;
using Simulation.ECS.Systems;
using Simulation.ECS.Systems.Handlers;
using Simulation.ECS.Systems.Index;

// Adicione todos os 'usings' para seus sistemas aqui
namespace Simulation.ECS;

/// <summary>
/// Define a ordem de execução de todos os sistemas da simulação a cada tick.
/// Utiliza um contêiner de DI exclusivo para construir e injetar as dependências
/// entre os sistemas de forma automática.
/// </summary>
public sealed class SimulationPipeline : Group<float>
{
    // O construtor agora é o responsável por "montar" o pipeline
    public SimulationPipeline(WorldOptions worldOptions, SpatialOptions spatialOptions, IServiceProvider mainAppServices) 
        : base("SimulationGroup")
    {
        // 1. Crie a instância do World primeiro.
        var world = World.Create(
            chunkSizeInBytes: worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: worldOptions.ArchetypeCapacity,
            entityCapacity: worldOptions.EntityCapacity
        );
        
        // 1. Cria uma ServiceCollection exclusiva para os sistemas ECS
        var systemServices = new ServiceCollection();
        
        // 2. Registre a INSTÂNCIA do World.
        systemServices.AddSingleton(world); 
        systemServices.AddSingleton(mainAppServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(mainAppServices.GetRequiredService<MapManagerService>());
        systemServices.AddSingleton(mainAppServices.GetRequiredService<NetManager>()); // Exemplo de outro serviço
        
        // 3. Registre todos os sistemas como Singletons
        // Para sistemas com dependências primitivas, use um factory lambda.
        systemServices.AddSingleton(sp => new SpatialIndexSystem(
            sp.GetRequiredService<World>(), 
            spatialOptions.Width, 
            spatialOptions.Height
        ));
        
        // Sistemas com dependências que já estão no contêiner podem ser registrados diretamente.
        systemServices.AddSingleton<PlayerIndexSystem>();
        systemServices.AddSingleton<MapIndexSystem>();
        systemServices.AddSingleton<PlayerInputCommandSystem>();
        systemServices.AddSingleton<PlayerLifecycleSystem>();
        systemServices.AddSingleton<GridMovementSystem>();
        systemServices.AddSingleton<TeleportSystem>();
        systemServices.AddSingleton<CooldownSystem>();
        systemServices.AddSingleton<CombatSystem>();
        systemServices.AddSingleton<NetworkSnapshotSystem>();

        // 5. Constrói o provedor de serviços exclusivo para o ECS
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        
        // 5. Método auxiliar para adicionar e registrar ao mesmo tempo
        void AddSystem<T>() where T : ISystem<float>
        {
            Add(ecsServiceProvider.GetRequiredService<T>());
        }

        // 6. Adiciona os sistemas ao grupo na ordem de execução correta
        //    usando o método auxiliar para manter o código limpo (DRY).
        AddSystem<PlayerIndexSystem>();
        AddSystem<MapIndexSystem>();
        AddSystem<SpatialIndexSystem>();
        
        AddSystem<PlayerInputCommandSystem>();
        AddSystem<PlayerLifecycleSystem>();
        
        AddSystem<GridMovementSystem>();
        AddSystem<TeleportSystem>();
        AddSystem<CombatSystem>();
        AddSystem<CooldownSystem>();
        
        AddSystem<NetworkSnapshotSystem>();
        
        // 7. Inicializa todos os sistemas
        Initialize();
    }
}