using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports.Char;
using Simulation.Core.Abstractions.Ports.Index;
using Simulation.Core.Abstractions.Ports.Map;

namespace Simulation.Core.Systems;

/// <summary>
/// Processa intenções de teleporte, validando a posição de destino
/// e atualizando o estado da entidade no mundo ECS.
/// </summary>
public sealed partial class TeleportSystem(
    World world,
    IMapIndex mapIndex,
    ISpatialIndex spatialIndex,
    ICharIndex charIndex,
    ILogger<TeleportSystem> logger)
    : BaseSystem<World, float>(world)
{
    private readonly List<Entity> _queryResults = new(16);

    [Query]
    [All<TeleportIntent, MapId, Position, CharId>]
    private void Process(in Entity entity, ref TeleportIntent intent, ref Position pos, in MapId mapId, in CharId charId)
    {
        var targetPos = intent.TargetPos;
        var targetMapId = intent.TargetMapId;

        // Valida o movimento antes de aplicá-lo
        if (IsTeleportInvalid(entity, targetMapId, targetPos))
        {
            logger.LogWarning("Teleporte inválido para CharId {CharId} para a posição {TargetPos} no mapa {TargetMapId}. Removendo intent.",
                charId.Value, targetPos, targetMapId);

            // Opcional: Enviar um snapshot de reconciliação para corrigir a posição do cliente, se necessário.
        }
        else
        {
            // O teleporte é válido, aplica as mudanças
            pos = targetPos;
            
            // Se o mapa mudou, atualiza o componente MapId na entidade
            if (mapId.Value != targetMapId)
            {
                World.Set(entity, new MapId(targetMapId));
            }

            // Marca a entidade como "suja" para que o índice espacial seja atualizado
            World.Add<SpatialDirty>(entity);

            // Envia um snapshot para notificar os clientes sobre o teleporte
            var snapshot = new TeleportSnapshot(charId.Value, mapId.Value, pos);
            EventBus.Send(in snapshot);

            logger.LogInformation("CharId {CharId} teletransportado para {Position} no mapa {MapId}",
                charId.Value, pos, targetMapId);
        }

        // Remove o componente de intenção, pois já foi processado
        World.Remove<TeleportIntent>(entity);
    }

    /// <summary>
    /// Verifica se uma posição de teleporte é válida, checando limites do mapa e colisões.
    /// </summary>
    private bool IsTeleportInvalid(Entity entity, int mapIdValue, Position targetPos)
    {
        if (!mapIndex.TryGetMap(mapIdValue, out var mapData))
        {
            logger.LogWarning("Tentativa de teleporte para mapa inválido: {MapId}", mapIdValue);
            return true; // Mapa de destino não existe
        }

        if (targetPos.X < 0 || targetPos.Y < 0 || targetPos.X >= mapData.Width || targetPos.Y >= mapData.Height)
        {
            logger.LogWarning("Tentativa de teleporte para fora dos limites do mapa: {Position}", targetPos);
            return true; // Fora dos limites do mapa
        }

        if (mapData.IsBlocked(targetPos))
        {
            return true; // Colisão com o terreno
        }

        _queryResults.Clear();
        spatialIndex.Query(targetPos, 0, _queryResults); // Query num raio de 0 para obter entidades no tile exato

        foreach (var otherEntity in _queryResults)
        {
            if (otherEntity == entity) continue; // Não colide consigo mesmo

            if (World.Has<Blocking>(otherEntity))
            {
                return true; // Colisão com outra entidade que bloqueia o movimento
            }
        }

        return false;
    }
}

