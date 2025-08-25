using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Systems;

namespace Simulation.Core;

public class SimulationPipeline(IServiceProvider provider) : List<BaseSystem<World, float>>
{
    public virtual void Configure()
    {
        // --- In ---
        // 1. Enfileira comandos vindos de fora do mundo ECS.
        Add(provider.GetRequiredService<IntentsEnqueueSystem>());
        
        // --- Processamento de Comandos ---
        // 2. Processa os comandos da fila, criando/alterando entidades.
        Add(provider.GetRequiredService<IntentsDequeueSystem>());
        
        // 3. Adiciona o sistema que faltava: carrega os mapas necessários.
        // Deve vir logo após o dequeue, caso um comando solicite o carregamento de um mapa.
        Add(provider.GetRequiredService<MapLoaderSystem>());
        
        // --- Lógica Principal da Simulação ---
        Add(provider.GetRequiredService<PlayerLifecycleSystem>()); // Adicionado
        Add(provider.GetRequiredService<SpawnDespawnSystem>());
        Add(provider.GetRequiredService<GridMovementSystem>());
        Add(provider.GetRequiredService<TeleportSystem>());
        Add(provider.GetRequiredService<AttackSystem>());
        
        // --- Finalização do Ciclo ---
        // Aplica as mudanças de posição no índice espacial.
        Add(provider.GetRequiredService<SpatialIndexCommitSystem>());
        
        // --- Out ---
        // Gera os "snapshots" ou eventos de saída para o mundo externo.
        Add(provider.GetRequiredService<SnapshotPostSystem>());
    }
}