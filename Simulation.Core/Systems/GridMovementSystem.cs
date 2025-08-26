using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging; // Adicionado
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

// Assinatura da classe atualizada para receber ILogger
public sealed partial class GridMovementSystem(World world, ISpatialIndex grid, ILogger<GridMovementSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<MoveIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    [All<MoveSpeed>]
    [All<TileVelocity>]
    private void ProcessIntent(in Entity entity, in MoveIntent cmd, in MapRef mapRef,
        ref TilePosition tilePos, ref MoveSpeed speed, ref TileVelocity vel)
    {
        if (cmd.Input.IsZero)
        {
            // Se o cliente enviar (0,0), paramos o personagem.
            vel.Velocity = VelocityVector.Zero;
        }
        else
        {
            // Garante velocidade consistente em todas as direções.
            var directionVector = new VelocityVector(cmd.Input.X, cmd.Input.Y);
            vel.Velocity = directionVector.Normalize() * speed.Value;
        }

        logger.LogInformation(
            "Entity {EntityId}: Processou MoveIntent com input {Input}. Nova TileVelocity = {Velocity}",
            entity.Id, cmd.Input, vel.Velocity
        );

        World.Remove<MoveIntent>(entity);
    }

    [Query]
    [All<MapRef>]
    [All<TilePosition>]
    [All<TileVelocity>]
    [All<MoveAccumulator>]
    private void ProcessMovement([Data] in float dt, in Entity e,
        ref TilePosition pos, ref TileVelocity vel, ref MapRef map, ref MoveAccumulator acc)
    {
        if (dt <= 0f) return;

        if (vel.Velocity.IsZero)
        {
            if (!acc.Value.IsZero)
            {
                logger.LogInformation("Entity {EntityId}: Velocidade zerada, limpando acumulador.", e.Id);
                acc.Value = VelocityVector.Zero;
            }
            return;
        }

        // 1. Pega o valor acumulado
        var accumulatedBefore = acc.Value;

        // 2. Adiciona o deslocamento
        var totalDisplacement = accumulatedBefore + (vel.Velocity * dt);

        // 3. Calcula o passo
        var step = new GameVector2((int)totalDisplacement.X, (int)totalDisplacement.Y);

        logger.LogTrace(
            "Entity {EntityId}: dt={DeltaTime:F4}, Vel={Velocity}, AccBefore={AccBefore}, TotalDisp={TotalDisplacement}, Step={Step}",
            e.Id, dt, vel.Velocity, accumulatedBefore, totalDisplacement, step
        );

        // 4. Tenta mover
        if (!step.IsZero)
        {
            TryMove(e, ref pos, map.MapId, step);
            totalDisplacement -= new VelocityVector(step.X, step.Y);
        }

        // 5. Guarda a parte fracionária
        acc.Value = totalDisplacement;
    }

    private void TryMove(Entity entity, ref TilePosition pos, int mapId, GameVector2 step)
    {
        var remainingStep = step;
        var currentPos = pos.Position;

        // Move no eixo X
        while (remainingStep.X != 0)
        {
            var singleStep = new GameVector2(Math.Sign(remainingStep.X), 0);
            var target = currentPos + singleStep;
            if (IsMoveInvalid(entity, mapId, target)) break;
            currentPos = target;
            remainingStep = remainingStep with { X = remainingStep.X - singleStep.X };
        }

        // Move no eixo Y
        while (remainingStep.Y != 0)
        {
            var singleStep = new GameVector2(0, Math.Sign(remainingStep.Y));
            var target = currentPos + singleStep;
            if (IsMoveInvalid(entity, mapId, target)) break;
            currentPos = target;
            remainingStep = remainingStep with { Y = remainingStep.Y - singleStep.Y };
        }
        
        var old = pos.Position;
        var newPos = currentPos;
        
        if (old == newPos)
        {
            logger.LogWarning("Entity {EntityId}: Tentou mover com step {Step}, mas movimento foi bloqueado. Posição permanece {Position}", entity.Id, step, old);
            return;
        }
        
        pos.Position = newPos;
        
        ref var dirty = ref World.AddOrGet<SpatialIndexDirty>(entity);
        dirty.Old = old;
        dirty.New = newPos;
        dirty.MapId = mapId;
        grid.EnqueueUpdate(entity.Id, mapId, old, newPos);
        
        var charId = World.Get<CharId>(entity);
        World.Add<MoveSnapshot>(entity, new MoveSnapshot(charId.CharacterId, newPos - old, newPos));
        
        logger.LogInformation(
            "Entity {EntityId}: MOVIMENTO EFETUADO de {OldPosition} para {NewPosition} (Step Solicitado: {Step})",
            entity.Id, old, newPos, step
        );
    }

    private bool IsMoveInvalid(Entity entity, int mapId, GameVector2 target)
    {
        if (!MapIndex.TryGetMap(mapId, out var map))
        {
            logger.LogWarning("Entity {EntityId}: Movimento inválido para {Target} - Mapa {MapId} não encontrado.", entity.Id, target, mapId);
            return true;
        }

        if (!map.InBounds(target))
        {
            logger.LogWarning("Entity {EntityId}: Movimento inválido para {Target} - Fora dos limites do mapa.", entity.Id, target);
            return true;
        }

        if (map.IsBlocked(target))
        {
            logger.LogWarning("Entity {EntityId}: Movimento inválido para {Target} - Bloqueado pela colisão do mapa.", entity.Id, target);
            return true;
        }

        var blockedByEntity = false;
        grid.QueryAABB(mapId, target.X, target.Y, target.X, target.Y, eid =>
        {
            if (World.Has<Blocking>(entity))
            {
                blockedByEntity = true;
            }
        });
        
        if (blockedByEntity)
        {
            logger.LogWarning("Entity {EntityId}: Movimento inválido para {Target} - Bloqueado por outra entidade.", entity.Id, target);
            return true;
        }

        return false;
    }
}