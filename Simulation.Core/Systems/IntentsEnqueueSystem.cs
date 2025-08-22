using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Simulation.Core.Abstractions.In;

namespace Simulation.Core.Systems;

public class IntentsEnqueueSystem(World world) : BaseSystem<World, float>(world), IIntentProducer
{
    // internal para acesso do DequeueSystem (ou make internal/protected e friend)
    internal readonly ConcurrentQueue<MoveIntent> MoveQueue = new();
    internal readonly ConcurrentQueue<TeleportIntent> TeleportQueue = new();
    internal readonly ConcurrentQueue<AttackIntent> AttackQueue = new();

    public void EnqueueMoveIntent(in MoveIntent intent)  => MoveQueue.Enqueue(intent);
    public void EnqueueTeleportIntent(in TeleportIntent intent) => TeleportQueue.Enqueue(intent);
    public void EnqueueAttackIntent(in AttackIntent intent) => AttackQueue.Enqueue(intent);
}