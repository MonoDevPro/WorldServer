using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Factories;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Map;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Systems;

public sealed partial class PlayerLifecycleSystem(
    World world,
    ICharIndex charIndex,
    ICharTemplateIndex charTemplateIndex,
    ISpatialIndex spatialIndex,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world: world)
{
    [Query]
    [All<EnterIntent>]
    [All<CharId>]
    [All<MapId>]
    [All<Position>]
    private void OnSpawnRequest(in Entity entity, in EnterIntent intent, in CharId cid, in MapId mid, in Position pst)
    {
        // 1. Registra a nova entidade nos índices
        charIndex.Register(cid.Value, entity);
        spatialIndex.Add(entity, pst);
        
        var mapId = mid.Value;
        var charId = cid.Value;
        var templates = new List<CharTemplate>();
        
        World.Query(in CharFactory.QueryDescription, (Entity e, ref MapId mid) =>
        {
            if (mid.Value == mapId)
            {
                var template = charTemplateIndex.TryGet(World.Get<CharId>(e).Value, out var existingTemplate)
                    ? existingTemplate
                    : null;
                template ??= CharFactory.CreateCharTemplate(World, e);
                templates.Add(template);
            }
        });
        
        // 2. Constrói os snapshots necessários usando a factory centralizada
        var enterSnapshot = SnapshotBuilder.CreateEnterSnapshot(World, entity, templates.ToArray());
        var charSnapshot = SnapshotBuilder.CreateCharSnapshot(World, entity,
            charTemplateIndex.TryGet(charId, out var tmp) ? tmp : null);
        
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
        
        var template = charTemplateIndex.TryGet(intent.CharId, out var existingTemplate) ? existingTemplate : null;
        template ??= CharFactory.CreateCharTemplate(World, e);
        
        // 2. Dispara o evento de saída ANTES de destruir a entidade.
        //    Usa o snapshot simples, que contém apenas o ID, pois é tudo que os clientes precisam.
        var exitSnapshot = SnapshotBuilder.CreateExitSnapshot(World, e, template);
        EventBus.Send(in exitSnapshot);
        
        // 3. Destrói a entidade, liberando seus componentes do mundo ECS.
        World.Destroy(e);
        
        charTemplateIndex.Unregister(intent.CharId);
        
        logger.LogInformation("Despawned CharId {CharId} (Entity {EntityId})", intent.CharId, e.Id);
    }
}