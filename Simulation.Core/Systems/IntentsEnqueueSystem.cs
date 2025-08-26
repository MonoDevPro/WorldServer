using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;

namespace Simulation.Core.Systems;

public class IntentsEnqueueSystem(World world, ILogger<IntentsEnqueueSystem> logger) : BaseSystem<World, float>(world), IIntentProducer
{
    // internal para acesso do DequeueSystem (ou make internal/protected e friend)
    internal readonly Queue<EnterGameIntent> EnterGameQueue = new();
    internal readonly Queue<ExitGameIntent> ExitGameQueue = new();
    internal readonly Queue<MoveIntent> MoveQueue = new();
    internal readonly Queue<TeleportIntent> TeleportQueue = new();
    internal readonly Queue<AttackIntent> AttackQueue = new();

    public void EnqueueEnterGameIntent(in EnterGameIntent intent) => EnterGameQueue.Enqueue(intent);
    public void EnqueueExitGameIntent(in ExitGameIntent intent) => ExitGameQueue.Enqueue(intent);
    public void EnqueueMoveIntent(in MoveIntent intent)  => MoveQueue.Enqueue(intent);
    public void EnqueueTeleportIntent(in TeleportIntent intent) => TeleportQueue.Enqueue(intent);
    public void EnqueueAttackIntent(in AttackIntent intent) => AttackQueue.Enqueue(intent);
}