using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

/// <summary>
/// Sistema que roda no loop do ECS e aplica MapData já carregados em memória
/// no World e no IMapIndex. É o único lugar que muta o World relativo a mapas.
/// Outros threads / serviços devem apenas enfileirar MapData aqui.
/// </summary>
public sealed class MapLoaderSystem(World world, IMapIndex mapIndex, ILogger<MapLoaderSystem> logger)
    : BaseSystem<World, float>(world), IMapLoaderSystem
{
    private readonly Queue<MapData> _mapQueue = new();
    private bool _disposed;

    /// <summary>
    /// Enfileira MapData vindo de outro thread (ex: MapLoaderService após I/O).
    /// </summary>
    public void EnqueueMapData(MapData mapData)
    {
        // dedupe agressivo opcional: se já está no index, ignore cedo
        if (mapIndex.TryGetMap(mapData.MapId, out _))
        {
            logger.LogDebug("EnqueueMapFromData: mapa {MapId} já registrado no index — ignorando enqueue.", mapData.MapId);
            return;
        }

        _mapQueue.Enqueue(mapData);
    }

    /// <summary>
    /// Roda no thread do jogo. Desenfileira todos MapData pendentes e aplica no World.
    /// </summary>
    public override void Update(in float delta)
    {
        // process all items currently queued (batch)
        while (_mapQueue.TryDequeue(out var mapData))
        {
            try
            {
                // Skip if already added concurrently by outro processamento
                if (mapIndex.TryGetMap(mapData.MapId, out _))
                {
                    logger.LogDebug("Update: mapa {MapId} já presente, pulando.", mapData.MapId);
                    continue;
                }

                // Register map components into the World (single place that mutates World)
                World.Create(
                    new MapId(mapData.MapId),
                    new MapSize(new GameSize(mapData.Width, mapData.Height)),
                    new MapFlags(mapData.UsePadded)
                );

                // add to index for quick lookups
                mapIndex.Add(mapData.MapId, mapData);

                logger.LogInformation("Mapa '{MapName}' ({Width}x{Height}) registrado no World e IMapIndex.", mapData.Name, mapData.Width, mapData.Height);
            }
            catch (Exception ex)
            {
                // Não deixe uma falha parar o loop: log e continue
                logger.LogError(ex, "Falha ao registrar mapa {MapId} no World.", mapData.MapId);
            }
        }
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // se precisar, limpe a fila:
        while (_mapQueue.TryDequeue(out _)) { }
        base.Dispose();
        
        GC.SuppressFinalize(this);
    }
}