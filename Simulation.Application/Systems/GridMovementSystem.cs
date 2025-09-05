using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Domain.Components;
using Simulation.Domain.Helpers;

namespace Simulation.Application.Systems;

/// <summary>
/// Gerencia a lógica de movimento baseado em grid, desde a intenção até a conclusão.
/// </summary>
public sealed partial class GridMovementSystem(
    World world,
    IMapServiceIndex mapServiceIndex,
    ISpatialIndex spatialIndex,
    ILogger<GridMovementSystem> logger)
    : BaseSystem<World, float>(world)
{
    private readonly List<Entity> _queryResults = new();

    /// <summary>
    /// Processa a intenção de mover, iniciando a ação de movimento.
    /// </summary>
    [Query]
    [All<MoveIntent, MapId, Position, MoveStats, CharId>]
    [None<MoveAction>] // Previne o início de um novo movimento se um já estiver ocorrendo.
    private void ProcessIntent(in Entity entity, in MoveIntent intent, in MapId mapId, in Position pos, in MoveStats stats, in CharId charId)
    {
        if (intent.Input.IsZero())
        {
            World.Remove<MoveIntent>(entity);
            return;
        }

        var startPos = pos;
        var targetPos = new Position { X = startPos.X + intent.Input.X, Y = startPos.Y + intent.Input.Y };

        // Valida se o movimento é possível.
        if (IsMoveInvalid(entity, mapId.Value, targetPos))
        {
            logger.LogWarning("CharId {charId}: Movimento inválido de {start} para {target}.", charId.Value, startPos, targetPos);
            // Envia um snapshot de reconciliação para corrigir a posição do cliente, caso ele tenha previsto o movimento.
            EventBus.Send(new MoveSnapshot(charId.Value, startPos, startPos));
            World.Remove<MoveIntent>(entity);
            return;
        }

        var distance = MathF.Sqrt(MathF.Pow(targetPos.X - startPos.X, 2) + MathF.Pow(targetPos.Y - startPos.Y, 2));
        var speed = stats.Speed > 0 ? stats.Speed : 1f;
        var duration = distance / speed;

        var moveAction = new MoveAction
        {
            Start = startPos,
            Target = targetPos,
            Elapsed = 0f,
            Duration = duration
        };
        World.Add(entity, moveAction);

        // Dispara o evento para a rede, notificando que o movimento começou.
        EventBus.Send(new MoveSnapshot(charId.Value, startPos, targetPos));
        
        World.Remove<MoveIntent>(entity);
        logger.LogInformation("CharId {id}: iniciou movimento de {start} para {target}.", charId.Value, startPos, targetPos);
    }

    /// <summary>
    /// Processa as ações de movimento em andamento.
    /// </summary>
    [Query]
    [All<Position, MoveAction>]
    private void ProcessMovement([Data] in float dt, in Entity entity, ref Position pos, ref MoveAction action)
    {
        action.Elapsed += dt;

        // O movimento só é concluído quando o tempo decorrido alcança a duração total.
        if (action.Elapsed < action.Duration)
        {
            // Opcional: Lógica de interpolação para uma posição visual "flutuante" poderia entrar aqui.
            return;
        }
        
        // Movimento concluído: atualiza a posição autoritativa.
        var oldPos = pos;
        pos = action.Target;

        // Marca a entidade como "suja" para que o SpatialIndexSyncSystem a atualize.
        World.Add<SpatialDirty>(entity);

        // Remove o componente de ação, permitindo novos movimentos.
        World.Remove<MoveAction>(entity);

        logger.LogInformation("Entidade {entityId}: completou movimento de {old} para {new}.", entity, oldPos, pos);
    }

    private bool IsMoveInvalid(Entity entity, int mapId, Position target)
    {
        if (!mapServiceIndex.TryGet(mapId, out var currentMap))
        {
            logger.LogWarning("Mapa {MapId} não encontrado para validação de movimento.", mapId);
            return true; // Se o mapa não existe, o movimento é inválido.
        }

        if (target.X < 0 || target.Y < 0 || target.X >= currentMap.Width || target.Y >= currentMap.Height)
        {
            return true; // Fora dos limites do mapa.
        }

        if (currentMap.IsBlocked(target))
        {
            return true; // Bloqueado por terreno estático.
        }
        
        _queryResults.Clear();
        spatialIndex.Query(target, 0, _queryResults); // Raio 0 para buscar apenas no tile exato.

        foreach(var otherEntity in _queryResults)
        {
            if (otherEntity == entity) continue; // Não colide consigo mesmo.
            if (World.Has<Blocking>(otherEntity))
            {
                return true; // Bloqueado por outra entidade.
            }
        }

        return false;
    }
}
