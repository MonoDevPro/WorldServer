using System.Numerics;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

/// <summary>
/// GridMovementSystem — versão enxuta e correta:
/// - cliente envia MoveIntent (direction)
/// - servidor calcula targetTile = current + sign(direction)
/// - se válido, cria MovementState com Duration = distance / speed
/// - ProcessMovement atualiza Elapsed e completa movimento quando Elapsed >= Duration
/// - ao completar, atualiza TilePosition (inteiro), marca SpatialIndexDirty e enfileira update no grid
/// </summary>
public sealed partial class GridMovementSystem(World world, ISpatialIndex grid, ILogger<GridMovementSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Recebe intents — tenta iniciar um movimento se a entidade não estiver já se movendo
    [Query]
    [All<MoveIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    [All<MoveSpeed>]
    private void ProcessIntent(in Entity entity, in MoveIntent intent, in MapRef mapRef, ref TilePosition pos, ref MoveSpeed speed)
    {
        // If zero input -> ignore (no-op)
        if (intent.Input.IsZero)
        {
            World.Remove<MoveIntent>(entity);
            return;
        }

        // If already moving, ignore new intents (simple policy)
        if (World.Has<MoveState>(entity))
        {
            logger.LogDebug("Entity {EntityId}: intent ignored because already moving.", entity.Id);
            World.Remove<MoveIntent>(entity);
            return;
        }

        // compute target tile: one step in sign(input)
        var dir = intent.Input.Sign();
        var start = pos.Position;
        var target = new GameVector2(start.X + dir.X, start.Y + dir.Y);

        // validate movement
        if (IsMoveInvalid(entity, mapRef.MapId, target))
        {
            logger.LogDebug("Entity {EntityId}: attempted move from {Start} to blocked target {Target}.", entity.Id, start, target);
            World.Remove<MoveIntent>(entity);
            return;
        }

        // compute distance (diagonal allowed) and duration based on speed (tiles/sec)
        var dx = target.X - start.X;
        var dy = target.Y - start.Y;
        var distance = MathF.Sqrt(dx*dx + dy*dy); // usually 1 or sqrt(2)
        var spd = speed.Value;
        if (spd <= 0f) spd = 1f; // fallback to avoid div0

        var duration = distance / spd;
        if (duration <= 0f) duration = 0.001f; // tiny epsilon to ensure progress

        var state = new MoveState
        {
            Start = start,
            Target = target,
            Elapsed = 0f,
            Duration = duration
        };

        World.Add<MoveState>(entity, state);
        logger.LogInformation("Entity {EntityId}: started movement {Start} -> {Target} duration={Duration:F3}s (speed={Speed})", entity.Id, start, target, duration, spd);

        // Remove the intent (consumed)
        World.Remove<MoveIntent>(entity);
        
        // If not completed, we could do nothing (tile pos remains old) or publish an interpolated snapshot for clients.
        // Example: if you want to send intermediate positions for smooth client visuals, add a MoveSnapshot component:
        if (World.Has<CharId>(entity))
        {
            var charId = World.Get<CharId>(entity).CharacterId;
            World.Add<MoveSnapshot>(entity, new MoveSnapshot(charId, start, target));
        }

        // Ensure entity is at least registered in the spatial index (if not already)
        try
        {
            if (!grid.IsRegistered(entity.Id))
                grid.Register(entity.Id, mapRef.MapId, start);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Entity {EntityId}: failed to ensure registration in spatial index.", entity.Id);
        }
    }

    // Process ongoing movements
    [Query]
    [All<MapRef>]
    [All<TilePosition>]
    [All<MoveState>]
    private void ProcessMovement([Data] in float dt, in Entity e, ref TilePosition pos, ref MoveState state,
        in MapRef mapRef)
    {
        if (dt <= 0f)
            return;

        state.Elapsed += dt;
        var t = state.Duration > 0f ? MathF.Min(state.Elapsed / state.Duration, 1f) : 1f;

        // Optionally we could expose a float position for interpolation (visual). Here we keep TilePosition integer,
        // and only update it when movement completes to preserve rule-consistency.
        if (!(t >= 1f))
            return;
        
        // Movement complete: update authoritative TilePosition (integer)
        var old = pos.Position;
        pos.Position = state.Target;

        // mark spatial index dirty + enqueue update (batch)
        ref var dirty = ref World.AddOrGet<SpatialIndexDirty>(e);
        dirty.Old = state.Start;
        dirty.New = state.Target;
        dirty.MapId = mapRef.MapId;

        grid.EnqueueUpdate(e.Id, mapRef.MapId, state.Start, state.Target);

        // remove movement state
        World.Remove<MoveState>(e);

        logger.LogInformation("Entity {EntityId}: completed movement {Old} -> {New}", e.Id, old, pos.Position);
    }

    // check static and dynamic blocking; uses spatial index for dynamic entities
    private bool IsMoveInvalid(Entity entity, int mapId, GameVector2 target)
    {
        if (!MapIndex.TryGetMap(mapId, out var map))
        {
            logger.LogWarning("Entity {EntityId}: map {MapId} not found.", entity.Id, mapId);
            return true;
        }

        if (target.X < 0 || target.Y < 0 || target.X >= map.Width || target.Y >= map.Height)
        {
            return true;
        }

        if (map.IsBlocked(target))
        {
            return true;
        }

        var blockedByEntity = false;
        grid.QueryAABB(mapId, target.X, target.Y, target.X, target.Y, eid =>
        {
            // ignore self if already registered on same tile
            if (eid == entity.Id) return;
            var other = grid.GetEntity(eid);
            if (World.Has<Blocking>(other))
            {
                blockedByEntity = true;
            }
        });

        return blockedByEntity;
    }

    // helper lerp if you want interpolation later
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
