using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Adapters;
using Simulation.Core.Systems;

namespace Simulation.Core;

public class SimulationPipeline : List<BaseSystem<World, float>>
{
    private readonly IServiceProvider _provider;

    public SimulationPipeline(IServiceProvider provider)
    {
        _provider = provider;

        Configure();
    }

    private void Configure()
    {
        // --- In ---
        // 0. Adiciona sistemas que aceitam dados externos (network, admin...) e enfileiram intents / map loads.
        Add(_provider.GetRequiredService<MapLoaderSystem>());
        
        // Recebe enter/exit intents (translates external sessions into intent-entities)
        Add(_provider.GetRequiredService<PlayerLifecycleSystem>());
        
        // System that reads network packets and enqueues intents into a safe queue (if you have one)
        Add(_provider.GetRequiredService<IntentEnqueueSystem>());
        
        // --- Processamento de Comandos ---
        // 2. Processa commands que foram enfileirados -> aplica intents, cria/destroi entidades
        Add(_provider.GetRequiredService<IntentsDequeueSystem>());
        
        // --- Lógica Principal da Simulação ---
        // Spawn/Despawn: colocar antes do movimento se você quer que spawns participem do mesmo tick.
        Add(_provider.GetRequiredService<PlayerSpawnSystem>());
        Add(_provider.GetRequiredService<PlayerDespawnSystem>());
        Add(_provider.GetRequiredService<LifetimeSystem>());
        
        // --- Lógica Principal da Simulação ---
        Add(_provider.GetRequiredService<GridMovementSystem>());
        Add(_provider.GetRequiredService<TeleportSystem>());
        Add(_provider.GetRequiredService<AttackSystem>());
        
        // --- Finalização do Ciclo ---
        // Aplica as mudanças de posição no índice espacial.
        Add(_provider.GetRequiredService<SpatialIndexCommitSystem>());
        
        // --- Out ---
        // Gera os "snapshots" ou eventos de saída para o mundo externo.
        Add(_provider.GetRequiredService<SnapshotPostSystem>());
        Add(_provider.GetRequiredService<SnapshotPublisherSystem>());
    }
}