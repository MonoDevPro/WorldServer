using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class GridMovementSystem(World world, ISpatialIndex grid)
    : BaseSystem<World, float>(world)
{
    private readonly ISpatialIndex _grid = grid;

    [Query]
    [All<MoveIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    [All<MoveSpeed>]
    [All<TileVelocity>]
    private void ProcessIntent(in Entity entity, in MoveIntent cmd, in MapRef mapRef,
        ref TilePosition tilePos, ref MoveSpeed speed, ref TileVelocity vel)
    {
        if (!cmd.Input.IsZero)
        {
            // Calcula a velocidade em tiles por segundo
            vel.Velocity = cmd.Input * speed.Value;
        }

        // Remove o comando de intenção de movimento após processar
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
        if (dt <= 0f || vel.Velocity.IsZero)
        {
            // Se a velocidade for zerada, removemos o acumulador para limpar o estado
            acc.Value = VelocityVector.Zero;
            return;
        }
        
        // 1. O valor acumulado já está em 'acc.Value'
        // 2. Adiciona o deslocamento do frame atual
        var totalDisplacement = acc.Value + (vel.Velocity * dt);

        // 3. Calcula quantos tiles inteiros a entidade deve se mover
        var step = new GameVector2((int)totalDisplacement.X, (int)totalDisplacement.Y);

        // 4. Se houver movimento a ser feito, executa-o
        if (!step.IsZero)
        {
            TryMove(e, ref pos, map.MapId, step);
            // Subtrai os tiles inteiros que foram "usados" para o movimento
            totalDisplacement -= new VelocityVector(step.X, step.Y);
        }

        // 5. Guarda de volta a parte fracionária
        acc.Value = totalDisplacement;
    }

    private void TryMove(Entity entity, ref TilePosition pos, int mapId, GameVector2 step)
    {
        // Esta implementação agora move um tile de cada vez para evitar "atravessar" paredes
        var remainingStep = step;
        var currentPos = pos.Position;

        // Move no eixo X
        while (remainingStep.X != 0)
        {
            var singleStep = new GameVector2(Math.Sign(remainingStep.X), 0);
            var target = currentPos + singleStep;

            if (IsMoveInvalid(entity, mapId, target)) 
                break; // Para se encontrar um obstáculo

            currentPos = target;
            remainingStep = remainingStep with { X = remainingStep.X - singleStep.X };
        }

        // Move no eixo Y
        while (remainingStep.Y != 0)
        {
            var singleStep = new GameVector2(0, Math.Sign(remainingStep.Y));
            var target = currentPos + singleStep;

            if (IsMoveInvalid(entity, mapId, target)) break; // Para se encontrar um obstáculo

            currentPos = target;
            remainingStep = remainingStep with { Y = remainingStep.Y - singleStep.Y };
        }
        
        var old = pos.Position; // antes de cálculo
        var newPos = currentPos; // depois de cálculo
        
        if (old == newPos)
            return; // Não houve movimento efetivo
        
        pos.Position = newPos;
        
        ref var dirty = ref World.AddOrGet<SpatialIndexDirty>(entity);
        dirty.Old = old;
        dirty.New = newPos;
        dirty.MapId = mapId;
        _grid.EnqueueUpdate(entity.Id, mapId, old, newPos);
        
        // Envia snapshot de movimento para a rede
        var charId = World.Get<CharId>(entity);
        World.Add<MoveSnapshot>(entity, new MoveSnapshot(charId.CharacterId, newPos - old, newPos));
        
        Console.WriteLine($"Entity {entity.Id} moved to {pos.Position} on map {mapId}");
    }

    private bool IsMoveInvalid(Entity entity, int mapId, GameVector2 target)
    {
        // Valida mapa e bounds
        if (!MapIndex.TryGetMap(mapId, out var map))
            return true;

        if (target.X < 0 || target.Y < 0 || target.X >= map.Width || target.Y >= map.Height)
            return true;

        // Verifica se o tile está bloqueado no mapa (colisões estáticas)
        if (map.IsBlocked(target))
            return true;

        // Verifica se existe entidade bloqueante no tile
        var blockedByEntity = false;
        _grid.QueryAABB(mapId, target.X, target.Y, target.X, target.Y, eid =>
        {
            // se a entidade possui componente Blocking então impede o movimento
            if (World.Has<Blocking>(entity))
            {
                blockedByEntity = true;
            }
        });

        return blockedByEntity;
    }
}
