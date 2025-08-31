using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Application.Systems;
using Simulation.Core.Systems;

namespace Simulation.Application.Services;

/// <summary>
/// Define a ordem de execução de todos os sistemas da simulação a cada tick.
/// A ordem é crítica para garantir um fluxo de dados previsível e estável.
/// </summary>
public class SimulationPipeline : List<BaseSystem<World, float>>
{
    public SimulationPipeline(IServiceProvider provider)
    {
        Configure(provider);
    }

    private void Configure(IServiceProvider provider)
    {
        // --- GRUPO 1: ENTRADA E PREPARAÇÃO ---
        // Sistemas que recebem dados do "mundo externo" (rede, arquivos) e os preparam para o ECS.
        Add(provider.GetRequiredService<IntentsHandlerSystem>()); // Enfileira intents da rede. Roda primeiro para pegar o input mais recente.
        Add(provider.GetRequiredService<MapLoaderSystem>());      // Processa mapas carregados e os adiciona ao World.
        
        // --- GRUPO 2: LÓGICA DE CICLO DE VIDA E AÇÕES ---
        // Sistemas que reagem a intents e iniciam ações no mundo.
        Add(provider.GetRequiredService<PlayerLifecycleSystem>()); // Processa Enter/Exit intents para criar/destruir entidades de jogador.
        Add(provider.GetRequiredService<GridMovementSystem>());    // Inicia e processa ações de movimento.
        Add(provider.GetRequiredService<TeleportSystem>());        // Processa ações de teleporte.
        Add(provider.GetRequiredService<AttackSystem>());          // Inicia e processa ações de ataque.
        Add(provider.GetRequiredService<LifetimeSystem>());        // Processa o tempo de vida de entidades temporárias (ex: projéteis, efeitos).

        // --- GRUPO 3: SINCRONIZAÇÃO E FINALIZAÇÃO ---
        // Sistemas que rodam no final do tick para garantir que os dados estejam consistentes.
        Add(provider.GetRequiredService<SpatialIndexSyncSystem>()); // Sincroniza o índice espacial com as posições atualizadas. ESSENCIAL rodar depois de todos os sistemas de movimento.
        
        // --- GRUPO 4: SAÍDA ---
        // Sistemas que publicam os resultados do tick para o "mundo externo".
        Add(provider.GetRequiredService<SnapshotPublisherSystem>()); // Coleta eventos do EventBus e os envia para a camada de rede.
    }
}
