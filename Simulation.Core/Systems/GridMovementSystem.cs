using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class GridMovementSystem(World world, BlockingIndex blocking, BoundsIndex bounds)
    : BaseSystem<World, float>(world)
{
    // Fractional accumulator per entity to map tiles/s into steps
    private readonly Dictionary<int, VelocityVector> _accum = new();
    
    public readonly record struct Move(Entity Entity, int MapId, DirectionInput Input);
    
    public bool Apply(in Move cmd)
    {
        var e = cmd.Entity;
        if (!World.IsAlive(e)) 
            return false; // Entidade não existe ou foi destruída
        
        // Verifica se a entrada de direção é válida
        if (cmd.Input.IsZero) return false;

        if (!World.Has<TilePosition, TileVelocity, MoveSpeed, MapRef>(e))
        {
            throw new InvalidOperationException(
                "Entity must have TilePosition, TileVelocity, MoveSpeed, and MapRef components to move.");
        } 

        // Verifica se o mapa existe
        if (!bounds.TryGet(cmd.MapId, out var b))
            return false;
        
        ref var currentInput = ref World.AddOrGet<DirectionInput>(e);
        // Atualiza a entrada de direção
        currentInput.Direction = cmd.Input.Direction;

        // Adiciona ou atualiza os componentes necessários
        World.Set<MapRef>(e, new MapRef { MapId = cmd.MapId });

        return true;
    }
    
    [Query]
    [All<DirectionInput, TilePosition, MoveSpeed, TileVelocity, MapRef>]
    private void ProcessDirectionInput(in Entity e,
        ref DirectionInput dir, ref MoveSpeed speed, ref TileVelocity vel)
    {
        if (speed.Value <= 0f || dir.IsZero)
        {
            vel.Velocity = VelocityVector.Zero;
            return;
        }
        
        // Calcula velocidade acumulada em tiles
        var displacement = dir.Direction.Normalize() * speed.Value;

        vel.Velocity = displacement;
        dir.Direction = VelocityVector.Zero; // Reseta a direção após processar
    }
    
    [Query]
    [All<TilePosition, TileVelocity, MapRef>]
    private void ProcessMovement([Data] in float dt, in Entity e,
        ref TilePosition pos, ref TileVelocity vel, ref MapRef map)
    {
        if (dt <= 0f || vel.Velocity.IsZero)
            return;
        
        // Rebuild indices if dirty
        bounds.RebuildIfDirty(World);
        blocking.RebuildIfDirty(World);
        
        if (vel.Velocity.IsZero)
            return; // Sem movimento
        
        if (!_accum.TryGetValue(e.Id, out var acum))
        {
            // Inicializa o acumulador se não existir
            acum = new VelocityVector(0f, 0f);
            _accum[e.Id] = acum;
        }
        
        var id = e.Id;
        var acc = vel.Velocity * dt;
        var step = new GameVector2((int)acc.X, (int)acc.Y);
        
        // Move per-axis (N,S,E,W) tiles; diagonals if both non-zero
        TryMove(ref pos, map.MapId, step);
        
        acc -= new VelocityVector(step.X, step.Y);
        
        // Atualiza o acumulador
        _accum[id] += acc;
    }

    private void TryMove(ref TilePosition pos, int mapId, GameVector2 step)
    {
        if (step.IsZero) return;
        var target = pos.Position + step.Sign();
        // Bounds check if any bounds entity exists for this map
        if (bounds.TryGet(mapId, out var bound))
            if (!bound.Contains(new TilePosition{ Position = target })) return;

        if (!blocking.IsBlocked(mapId, target))
            pos.Position = target; // Move if not blocked
    }
}