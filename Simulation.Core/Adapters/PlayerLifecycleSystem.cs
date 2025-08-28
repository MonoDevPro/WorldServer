using System;
using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public sealed partial class PlayerLifecycleSystem(
    World world,
    PlayerSpawnSystem playerSpawnSystem,
    PlayerDespawnSystem playerDespawnSystem,
    IEntityIndex entityIndex,
    ICharIndex charIndex,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world), ILifecycleSystem
{
    // Pending spawn templates keyed by CharId (thread-safe)
    private readonly ConcurrentDictionary<int, CharTemplate> _pendingSpawns = new();
    // Simple queue for despawn requests (thread-safe)
    private readonly ConcurrentQueue<int> _pendingDespawns = new();

    private bool _disposed;

    /// <summary>
    /// Called by adapters (network/admin) to provide a prepared template for a charId.
    /// Thread-safe.
    /// </summary>
    public void EnqueueSpawn(CharTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        var cid = template.CharId.Value;
        if (cid <= 0) throw new ArgumentException("CharId must be > 0", nameof(template));
        _pendingSpawns[cid] = template; // upsert
        logger.LogDebug("Template enqueued for CharId {CharId}", cid);
    }

    /// <summary>
    /// Called by adapters to request a despawn by CharId (thread-safe).
    /// </summary>
    public void EnqueueDespawnByCharId(int charId)
    {
        if (charId <= 0) return;
        _pendingDespawns.Enqueue(charId);
        logger.LogDebug("Despawn enqueued for CharId {CharId}", charId);
    }

    public override void Update(in float delta)
    {
        // Process queued despawns -> forward to playerDespawnSystem
        while (_pendingDespawns.TryDequeue(out var dchar))
        {
            // Resolve entity (if still present)
            if (entityIndex.TryGetByCharId(dchar, out var ent))
            {
                playerDespawnSystem.EnqueueDespawn(ent);
            }
            else
            {
                logger.LogDebug("Despawn requested but CharId {CharId} not present; ignoring.", dchar);
            }
        }

        // NO BROAD processing of spawnQueue here: spawns are triggered by EnterGameIntent handler below.
        // But we could optionally flush stale pending templates if wanted.
    }

    [Query]
    [All<EnterGameIntent>]
    private void OnEnterGame(in Entity intentEntity, in EnterGameIntent intent)
    {
        var charId = intent.CharacterId;
        try
        {
            // First try to get a pre-enqueued template for that charId
            if (_pendingSpawns.TryRemove(charId, out var preTemplate))
            {
                logger.LogDebug("Found pre-enqueued template for CharId {CharId}. Proceeding to spawn.", charId);
                playerSpawnSystem.EnqueueSpawn(preTemplate);
                World.Destroy(intentEntity);
                return;
            }

            // If none, try to create template from charIndex prototype (fallback)
            if (charIndex.TryGetCharTemplate(charId, out var prototype) && prototype != null)
            {
                var runtimeTemplate = BuildRuntimeTemplateFromPrototype(prototype, charId);
                playerSpawnSystem.EnqueueSpawn(runtimeTemplate);
                World.Destroy(intentEntity);
                return;
            }

            // Last fallback: create minimal template
            var minimal = new CharTemplate
            {
                Name = $"Player{charId}",
                Gender = Simulation.Core.Abstractions.Adapters.Data.Gender.None,
                Vocation = Simulation.Core.Abstractions.Adapters.Data.Vocation.None,
                CharId = new Simulation.Core.Abstractions.Commons.CharId(charId),
                MapId = new Simulation.Core.Abstractions.Commons.MapId(1),
                Position = new Simulation.Core.Abstractions.Commons.Position { Value = new Simulation.Core.Abstractions.Commons.GameCoord(10,10) },
                Direction = new Simulation.Core.Abstractions.Commons.Direction { Value = new Simulation.Core.Abstractions.Commons.GameDirection(0,1) },
                MoveStats = new Simulation.Core.Abstractions.Commons.MoveStats { Speed = 1f },
                AttackStats = new Simulation.Core.Abstractions.Commons.AttackStats { CastTime = 0.5f, Cooldown = 1f }
            };
            playerSpawnSystem.EnqueueSpawn(minimal);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while handling EnterGame for CharId {CharId}", charId);
        }
        finally
        {
            World.Destroy(intentEntity);
        }
    }

    [Query]
    [All<ExitGameIntent>]
    private void OnExitGame(in Entity intentEntity, in ExitGameIntent intent)
    {
        var charId = intent.CharacterId;
        try
        {
            if (entityIndex.TryGetByCharId(charId, out var ent))
            {
                // enqueue despawn by entity
                playerDespawnSystem.EnqueueDespawn(ent);
            }
            else
            {
                logger.LogDebug("ExitGameIntent: CharId {CharId} not found in index, skipping despawn.", charId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while handling ExitGame for CharId {CharId}", charId);
        }
        finally
        {
            World.Destroy(intentEntity);
        }
    }

    // Helper: create a runtime instance (copy) of prototype and set runtime fields
    private static CharTemplate BuildRuntimeTemplateFromPrototype(CharTemplate prototype, int charId)
    {
        return new CharTemplate
        {
            Name = prototype.Name,
            Gender = prototype.Gender,
            Vocation = prototype.Vocation,
            CharId = prototype.CharId,
            MapId = prototype.MapId,
            Position = prototype.Position,
            Direction = prototype.Direction,
            MoveStats = prototype.MoveStats,
            AttackStats = prototype.AttackStats,
            Blocking = prototype.Blocking
        };
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pendingSpawns.Clear();
        while (_pendingDespawns.TryDequeue(out _)) { }
        base.Dispose();
    }
}
