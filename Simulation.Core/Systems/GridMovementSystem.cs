using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class GridMovementSystem(World world, BlockingIndex blocking, BoundsIndex bounds)
    : BaseSystem<World, float>(world)
{
    // Acumulador fracionário por entidade para mapear tiles/s em passos discretos
    private readonly Dictionary<int, VelocityVector> _accumulators = new();

    [Query]
    [All<MoveIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    [All<MoveSpeed>]
    [All<TileVelocity>]
    private void Process(in Entity entity, in MoveIntent cmd, in MapRef mapRef,
        ref TilePosition tilePos, ref MoveSpeed speed, ref TileVelocity vel)
    {
        if (!cmd.Input.IsZero)
        {
            // Calcula a velocidade em tiles por segundo
            vel.Velocity = cmd.Input * speed.Value;
            
            // Adiciona o comando de snapshot de movimento, para que o network possa enviar a posição atualizada
            World.Add<MoveSnapshot>(entity, new MoveSnapshot(cmd.CharId, cmd.Input, tilePos.Position));
        }
        
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

        bounds.RebuildIfDirty(World);
        blocking.RebuildIfDirty(World);

        // 1. Pega o valor acumulado da última atualização (ou zero)
        if (!_accumulators.TryGetValue(e.Id, out var accumulated))
        {
            accumulated = VelocityVector.Zero;
        }

        // 2. Adiciona o deslocamento do frame atual ao acumulador
        var totalDisplacement = accumulated + (vel.Velocity * dt);

        // 3. Calcula quantos tiles inteiros a entidade deve se mover
        var step = new GameVector2((int)totalDisplacement.X, (int)totalDisplacement.Y);
        
        // 4. Se houver movimento a ser feito, executa-o
        if (!step.IsZero)
        {
            TryMove(ref pos, map.MapId, step);
            // Subtrai os tiles inteiros que foram "usados" para o movimento
            totalDisplacement -= new VelocityVector(step.X, step.Y);
        }

        // 5. Guarda de volta a parte fracionária para o próximo frame
        _accumulators[e.Id] = totalDisplacement;
    }

    private void TryMove(ref TilePosition pos, int mapId, GameVector2 step)
    {
        // Esta implementação agora move um tile de cada vez para evitar "atravessar" paredes
        var remainingStep = step;
        var currentPos = pos.Position;

        // Move no eixo X
        while (remainingStep.X != 0)
        {
            var singleStep = new GameVector2(Math.Sign(remainingStep.X), 0);
            var target = currentPos + singleStep;

            if (IsMoveInvalid(mapId, target)) break; // Para se encontrar um obstáculo
            
            currentPos = target;
            
            remainingStep = remainingStep with { X = remainingStep.X - singleStep.X };
        }
        
        // Move no eixo Y
        while (remainingStep.Y != 0)
        {
            var singleStep = new GameVector2(0, Math.Sign(remainingStep.Y));
            var target = currentPos + singleStep;

            if (IsMoveInvalid(mapId, target)) break; // Para se encontrar um obstáculo

            currentPos = target;
            
            remainingStep = remainingStep with { Y = remainingStep.Y - singleStep.Y };
        }
        
        pos.Position = currentPos; // Atualiza a posição final
        Console.WriteLine($"Entity moved to {pos.Position} on map {mapId}");
    }

    private bool IsMoveInvalid(int mapId, GameVector2 target)
    {
        // Verifica limites do mapa
        if (bounds.TryGet(mapId, out var bound))
        {
            if (!bound.Contains(new TilePosition { Position = target })) return true;
        }
        // Verifica se o tile está bloqueado
        return blocking.IsBlocked(mapId, target);
    }
}