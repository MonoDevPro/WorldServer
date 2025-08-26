using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
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
        // 1. Enfileira comandos vindos de fora do mundo ECS.
        Add(_provider.GetRequiredService<IntentsEnqueueSystem>());
        
        // --- Processamento de Comandos ---
        // 2. Processa os comandos da fila, criando/alterando entidades.
        Add(_provider.GetRequiredService<IntentsDequeueSystem>());
        
        // 3. Adiciona o sistema que faltava: carrega os mapas necessários.
        // Deve vir logo após o dequeue, caso um comando solicite o carregamento de um mapa.
        Add(_provider.GetRequiredService<MapLoaderSystem>());
        
        // --- Lógica Principal da Simulação ---
        Add(_provider.GetRequiredService<PlayerLifecycleSystem>()); // Adicionado
        Add(_provider.GetRequiredService<SpawnDespawnSystem>());
        Add(_provider.GetRequiredService<GridMovementSystem>());
        Add(_provider.GetRequiredService<TeleportSystem>());
        Add(_provider.GetRequiredService<AttackSystem>());
        
        // --- Finalização do Ciclo ---
        // Aplica as mudanças de posição no índice espacial.
        Add(_provider.GetRequiredService<SpatialIndexCommitSystem>());
        
        // --- Out ---
        // Gera os "snapshots" ou eventos de saída para o mundo externo.
        Add(_provider.GetRequiredService<SnapshotPostSystem>());
    }
}