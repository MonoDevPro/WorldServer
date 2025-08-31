using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Factories;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Index;
using Simulation.Domain.Components;

namespace Simulation.Application.Systems;

public sealed partial class PlayerLifecycleSystem(
    World world,
    ICharIndex charIndex,
    ISpatialIndex spatialIndex,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<EnterIntent>]
    private void OnSpawnRequest(in Entity entity, in EnterIntent intent)
    {
        // 1. Registra a nova entidade nos índices
        var charId = World.Get<CharId>(entity).Value;
        var position = World.Get<Position>(entity);
        charIndex.Register(charId, entity);
        spatialIndex.Add(entity, position);

        // 2. Constrói os snapshots necessários
        var enterSnapshot = SnapshotBuilder.CreateEnterSnapshot(World, entity);
        var charSnapshot = SnapshotBuilder.CreateCharSnapshot(World, entity);
            
        // 3. Dispara os eventos. O SnapshotPublisherSystem irá capturá-los.
        EventBus.Send(in enterSnapshot); // Para o jogador que entrou
        EventBus.Send(in charSnapshot);  // Para os outros jogadores

        // 4. Remove o componente de intenção, pois já foi processado
        World.Remove<EnterIntent>(entity);
    }

    [Query]
    [All<ExitIntent>]
    private void OnDespawnRequest(in Entity e, in ExitIntent intent)
    {
        // 1. Remove a entidade dos índices para que não seja mais encontrada.
        charIndex.Unregister(intent.CharId);
        spatialIndex.Remove(e);
        
        // 2. Dispara o evento de saída ANTES de destruir a entidade.
        //    Isso garante que outros sistemas possam reagir ao evento no mesmo frame, se necessário.
        EventBus.Send(new ExitSnapshot(intent.CharId));
        
        // 3. Destrói a entidade, liberando seus componentes do mundo ECS.
        World.Destroy(e);
        
        logger.LogInformation("Despawned CharId {CharId} (Entity {EntityId})", intent.CharId, e.Id);
    }
}

