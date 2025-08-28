using System;
using System.Collections.Concurrent;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public sealed class PlayerDespawnSystem : BaseSystem<World, float>, IDisposable
{
    private readonly ConcurrentQueue<Entity> _queue = new();
    private readonly ICharIndex _charIndex;
    private readonly IEntityIndex _entityIndex;
    private readonly ISpatialIndex _spatialIndex;
    private readonly ISnapshotPublisher _publisher;
    private readonly ILogger<PlayerDespawnSystem> _logger;
    private bool _disposed;

    public PlayerDespawnSystem(World world, ICharIndex charIndex, IEntityIndex entityIndex, ISpatialIndex spatialIndex, ISnapshotPublisher publisher, ILogger<PlayerDespawnSystem> logger)
        : base(world)
    {
        _charIndex = charIndex;
        _entityIndex = entityIndex;
        _spatialIndex = spatialIndex;
        _publisher = publisher;
        _logger = logger;
    }

    public void EnqueueDespawn(Entity entity)
    {
        if (entity == default) return;
        _queue.Enqueue(entity);
    }

    public override void Update(in float delta)
    {
        while (_queue.TryDequeue(out var ent))
        {
            try
            {
                if (!World.IsAlive(ent)) continue;

                var mapId = World.Get<MapId>(ent);
                var charId = World.Get<CharId>(ent);

                // unregister first
                _entityIndex.UnregisterEntity(ent);

                if (_spatialIndex.IsRegistered(ent.Id))
                {
                    var maybeMap = _spatialIndex.GetEntityMap(ent.Id);
                    if (maybeMap.HasValue)
                        _spatialIndex.Unregister(ent.Id, maybeMap.Value);
                }

                // destroy entity
                World.Destroy(ent);

                _logger.LogInformation("Despawned CharId {CharId} (Entity {EntityId}).", charId.Value, ent.Id);

                // send exit snapshot/event
                World.Create( new ExitSnapshot(charId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing despawn for Entity {EntityId}", ent.Id);
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
