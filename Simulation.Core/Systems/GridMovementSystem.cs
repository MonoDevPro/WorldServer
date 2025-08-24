using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Commons.Components;
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
    // Accumulador fracionário por entidade para mapear tiles/s em passos discretos
    private readonly Dictionary<int, VelocityVector> _accumulators = new();
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

            // Envia snapshot de movimento para a rede
            World.Add<MoveSnapshot>(entity, new MoveSnapshot(cmd.CharId, cmd.Input, tilePos.Position));
        }

        // Garantir que a entidade esteja registrada no índice espacial
        // Chamamos Unregister antes para evitar entradas duplicadas caso já esteja registrada.
        try { _grid.Unregister(entity.Id, mapRef.MapId); }
        catch { /* Unregister é seguro se não existir; ignorar erros não-fatais */ }
        _grid.Register(entity.Id, mapRef.MapId, tilePos.Position);

        // Remove o comando de intenção de movimento após processar
        World.Remove<MoveIntent>(entity);
    }

    [Query]
    [All<MapRef>]
    [All<TilePosition>]
    [All<TileVelocity>]
    private void ProcessMovement([Data] in float dt, in Entity e, ref TilePosition pos, ref TileVelocity vel, ref MapRef map)
    {
        if (dt <= 0f || vel.Velocity.IsZero)
        {
            // Se a velocidade for zerada, removemos o acumulador para limpar o estado
            _accumulators.Remove(e.Id);
            return;
        }

        // Garantir que a entidade esteja registrada no índice espacial
        // (Unregister é idempotente / seguro)
        try { _grid.Unregister(e.Id, map.MapId); } catch { }
        _grid.Register(e.Id, map.MapId, pos.Position);

        // 1. Pega o valor acumulado da última atualização (ou zero)
        if (!_accumulators.TryGetValue(e.Id, out var accumulated))
            accumulated = VelocityVector.Zero;

        // 2. Adiciona o deslocamento do frame atual ao acumulador
        var totalDisplacement = accumulated + (vel.Velocity * dt);

        // 3. Calcula quantos tiles inteiros a entidade deve se mover
        var step = new GameVector2((int)totalDisplacement.X, (int)totalDisplacement.Y);

        // 4. Se houver movimento a ser feito, executa-o
        if (!step.IsZero)
        {
            TryMove(e, ref pos, map.MapId, step);
            // Subtrai os tiles inteiros que foram "usados" para o movimento
            totalDisplacement -= new VelocityVector(step.X, step.Y);
        }

        // 5. Guarda de volta a parte fracionária para o próximo frame
        _accumulators[e.Id] = totalDisplacement;
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
        pos.Position = newPos;
        
        ref var dirty = ref World.AddOrGet<SpatialIndexDirty>(entity);
        dirty.Old = old;
        dirty.New = newPos;
        dirty.MapId = mapId;
        _grid.EnqueueUpdate(entity.Id, mapId, old, newPos);

        
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
