using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Systems;

/// <summary>
/// GridMovementSystem — versão enxuta e correta:
/// - cliente envia MoveIntent (direction)
/// - servidor calcula targetTile = current + sign(direction)
/// - se válido, cria MovementState com Duration = distance / speed
/// - ProcessMovement atualiza Elapsed e completa movimento quando Elapsed >= Duration
/// - ao completar, atualiza TilePosition (inteiro), marca SpatialIndexDirty e enfileira update no grid
/// </summary>
public sealed partial class GridMovementSystem(World world, IMapIndex map, ISpatialIndex grid, IEntityIndex indexer, ILogger<GridMovementSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Recebe intents — tenta iniciar um movimento se a entidade não estiver já se movendo
    [Query]
    [All<MoveIntent>]
    [All<MapId>]
    [All<Position>]
    [All<MoveStats>]
    private void ProcessIntent(in Entity entity, in MoveIntent intent, in MapId mapId, ref Position pos, ref MoveStats stats)
    {
        // If zero input -> ignore (no-op)
        if (intent.Input.IsZero())
        {
            World.Remove<MoveIntent>(entity);
            return;
        }

        // If already moving, ignore new intents (simple policy)
        if (World.Has<MoveAction>(entity))
        {
            logger.LogDebug("Entity {EntityId}: intent ignored because already moving.", entity.Id);
            World.Remove<MoveIntent>(entity);
            return;
        }

        // compute target tile: one step in sign(input)
        var dir = intent.Input.Sign();
        var start = pos.Value;
        var target = new GameCoord(start.X + dir.X, start.Y + dir.Y);

        // validate movement
        if (IsMoveInvalid(entity, mapId.Value, target))
        {
            logger.LogInformation("Entity {EntityId}: attempted move from {Start} to blocked target {Target}.", entity.Id, start, target);
            World.Remove<MoveIntent>(entity);
            
            // Envia reconciliação
            var charId = World.Get<CharId>(entity).Value;
            World.Add<MoveSnapshot>(entity, new MoveSnapshot(charId, start, start));
            return;
        }

        // compute distance (diagonal allowed) and duration based on speed (tiles/sec)
        var dx = target.X - start.X;
        var dy = target.Y - start.Y;
        var distance = MathF.Sqrt(dx*dx + dy*dy); // usually 1 or sqrt(2)
        var spd = stats.Speed;
        if (spd <= 0f) spd = 1f; // fallback to avoid div0

        var duration = distance / spd;
        if (duration <= 0f) duration = 0.001f; // tiny epsilon to ensure progress

        var state = new MoveAction
        {
            Start = start,
            Target = target,
            Elapsed = 0f,
            Duration = duration
        };

        World.Add<MoveAction>(entity, state);
        logger.LogInformation("Entity {EntityId}: started movement {Start} -> {Target} duration={Duration:F3}s (speed={Speed})", entity.Id, start, target, duration, spd);

        // Remove the intent (consumed)
        World.Remove<MoveIntent>(entity);
        
        // If not completed, we could do nothing (tile pos remains old) or publish an interpolated snapshot for clients.
        // Example: if you want to send intermediate positions for smooth client visuals, add a MoveSnapshot component:
        if (World.Has<CharId>(entity))
        {
            var charId = World.Get<CharId>(entity).Value;
            World.Add<MoveSnapshot>(entity, new MoveSnapshot(charId, start, target));
        }

        // Ensure entity is at least registered in the spatial index (if not already)
        try
        {
            if (!grid.IsRegistered(entity.Id))
                grid.Register(entity.Id, mapId.Value, start);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Entity {EntityId}: failed to ensure registration in spatial index.", entity.Id);
        }
    }

    // Process ongoing movements
    [Query]
    [All<MapId>]
    [All<Position>]
    [All<MoveAction>]
    private void ProcessMovement([Data] in float dt, in Entity e, ref Position pos, ref MoveAction action,
        in MapId mapId)
    {
        if (dt <= 0f)
            return;

        action.Elapsed += dt;
        var t = action.Duration > 0f ? MathF.Min(action.Elapsed / action.Duration, 1f) : 1f;

        // Optionally we could expose a float position for interpolation (visual). Here we keep TilePosition integer,
        // and only update it when movement completes to preserve rule-consistency.
        if (!(t >= 1f))
            return;
        
        // Movement complete: update authoritative TilePosition (integer)
        var old = pos.Value;
        pos.Value = action.Target;

        // mark spatial index dirty + enqueue update (batch)
        World.Add<SpatialDirty>(e, new SpatialDirty(action.Start, action.Target));

        grid.EnqueueUpdate(e.Id, mapId.Value, action.Start, action.Target);

        // remove movement state
        World.Remove<MoveAction>(e);

        logger.LogInformation("Entity {EntityId}: completed movement {Old} -> {New}", e.Id, old, pos.Value);
    }

    // check static and dynamic blocking; uses spatial index for dynamic entities
    private bool IsMoveInvalid(Entity entity, int mapId, GameCoord target)
    {
        if (!map.TryGetMap(mapId, out var currentMap))
        {
            logger.LogWarning("Entity {EntityId}: map {MapId} not found.", entity.Id, mapId);
            return true;
        }

        if (target.X < 0 || target.Y < 0 || target.X >= currentMap.Width || target.Y >= currentMap.Height)
        {
            return true;
        }

        if (currentMap.IsBlocked(target))
        {
            return true;
        }

        var blockedByEntity = false;
        grid.QueryAABB(mapId, target.X, target.Y, target.X, target.Y, eid =>
        {
            // ignore self if already registered on same tile
            if (eid == entity.Id) return;
            
            // entity not found
            if (!indexer.TryGetByEntityId(eid, out var other))
                return;
            
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