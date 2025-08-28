using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public sealed class PlayerSpawnSystem(
    World world,
    ICharIndex charIndex,
    IEntityIndex entityIndex,
    ISpatialIndex spatialIndex,
    ILogger<PlayerSpawnSystem> logger)
    : BaseSystem<World, float>(world), IDisposable
{
    private readonly ConcurrentQueue<CharTemplate> _queue = new();
    private bool _disposed;

    public void EnqueueSpawn(CharTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        _queue.Enqueue(template);
    }

    public override void Update(in float delta)
    {
        while (_queue.TryDequeue(out var template))
        {
            try
            {
                var cid = template.CharId.Value;
                if (entityIndex.TryGetByCharId(cid, out var existing))
                {
                    logger.LogWarning("SpawnSystem: CharId {CharId} already exists as Entity {EntityId}, skipping spawn.", cid, existing.Id);
                    continue;
                }

                var created = World.Create(
                    template.CharId,
                    template.MapId,
                    template.Position,
                    template.Direction,
                    template.MoveStats,
                    template.AttackStats,
                    template.Blocking
                );

                entityIndex.Register(cid, created);
                spatialIndex.Register(created.Id, template.MapId.Value, template.Position.Value);

                logger.LogInformation("Spawned CharId {CharId} (Entity {EntityId}) at Map {MapId} Pos ({X},{Y})",
                    cid, created.Id, template.MapId.Value, template.Position.Value.X, template.Position.Value.Y);

                // Build snapshot: gather all players in same map
                var list = new List<CharTemplate>();
                // prototype + runtime values for all chars in map
                var q = new QueryDescription().WithAll<CharId, MapId>();
                World.Query(in q, (ref Entity e, ref CharId ch, ref MapId mp) =>
                {
                    if (mp.Value != template.MapId.Value) return;
                    // get prototype
                    if (charIndex.TryGetCharTemplate(ch.Value, out var proto) && proto != null)
                    {
                        // make a copy and fill runtime fields
                        var runtime = new CharTemplate
                        {
                            Name = proto.Name,
                            Gender = proto.Gender,
                            Vocation = proto.Vocation,
                            CharId = ch,
                            MapId = mp,
                            Position = World.Get<Position>(e),
                            Direction = World.Get<Direction>(e),
                            MoveStats = World.Get<MoveStats>(e),
                            AttackStats = World.Get<AttackStats>(e),
                            Blocking = World.Has<Blocking>(e) ? new Blocking() : default
                        };
                        list.Add(runtime);
                    }
                });

                // publish EnterSnapshot to the newly spawned client (publisher decides peer)
                var charsArray = list.ToArray();
                
                World.Create(new EnterSnapshot(cid, charsArray));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing spawn for CharId {CharId}", template?.CharId.Value);
            }
        }
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        while (_queue.TryDequeue(out _)) { }
        base.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
