using System.Collections.Concurrent;
using Arch.Buffer;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Factories;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Application.Services;
using Simulation.Domain.Components;

namespace Simulation.Application.Systems.In;

/// <summary>
/// Sistema que roda no loop do ECS e aplica MapData já carregados em memória
/// no World e no IMapIndex. É o único lugar que muta o World relativo a mapas.
/// Outros threads / serviços devem apenas enfileirar MapData aqui.
/// </summary>
public sealed class MapIntentHandlerSystem(World world, IMapIndex mapIndex, IMapTemplateIndex templateIndex, ILogger<MapIntentHandlerSystem> logger)
    : BaseSystem<World, float>(world), IMapIntentHandler
{
    private readonly CommandBuffer _cmd = new(256);
    
    private readonly ConcurrentQueue<LoadMapIntent> _loadQueue = new();
    private readonly ConcurrentQueue<UnloadMapIntent> _unloadQueue = new();
    
    public void HandleIntent(in LoadMapIntent intent) => _loadQueue.Enqueue(intent);
    public void HandleIntent(in UnloadMapIntent intent) => _unloadQueue.Enqueue(intent);

    /// <summary>
    /// Roda no thread do jogo. Desenfileira todos MapData pendentes e aplica no World.
    /// </summary>
    public override void Update(in float delta)
    {
        ConsumeLoadIntents();
        ConsumeUnloadIntents();
        
        _cmd.Playback(World, dispose: true);
    }
    
    private void ConsumeLoadIntents()
    {
        while (_loadQueue.TryDequeue(out var intent))
        {
            // Skip if already added concurrently by outro processamento
            if (mapIndex.TryGet(intent.MapId, out _))
            {
                logger.LogDebug("Update: mapa {MapId} já presente, pulando.", intent.MapId);
                continue;
            }
            // Load MapData from template repository
            if (!templateIndex.TryGet(intent.MapId, out var mapTemplate) || mapTemplate == null) 
            {
                logger.LogWarning("Mapa {MapId} não encontrado no repositório de templates.", intent.MapId);
                continue;
            }
            
            // Register map components into the World (single place that mutates World)
            var mapEntity = MapFactory.CreateEntity(_cmd, mapTemplate);
            _cmd.Add(mapEntity, intent);

            mapIndex.Register(mapTemplate.MapId, MapService.CreateFromTemplate(mapTemplate));
            logger.LogInformation("Mapa '{MapName}' ({Width}x{Height}) carregado no World.", mapTemplate.Name, mapTemplate.Width, mapTemplate.Height);
            
            // Send LoadMapSnapshot event
            EventBus.Send(new LoadMapSnapshot { MapId = mapTemplate.MapId });
        }
    }
    
    private void ConsumeUnloadIntents()
    {
        while (_unloadQueue.TryDequeue(out var intent))
        {
            // Remove do World
            if (mapIndex.TryGet(intent.MapId, out var mapService))
            {
                var result = intent;
                World.Query(in MapFactory.QueryDescription, (ref Entity entity, ref MapId mapId) =>
                {
                    if (mapId.Value != result.MapId) 
                        return;
                    _cmd.Destroy(entity);
                    EventBus.Send(new UnloadMapSnapshot { MapId = result.MapId });
                });
            }
        }
    }

    public override void Dispose()
    {
        // Limpa filas
        _loadQueue.Clear();
        _unloadQueue.Clear();
        // Limpa CommandBuffer
        _cmd.Dispose();
        // Chama Dispose da base
        base.Dispose();
    }
}