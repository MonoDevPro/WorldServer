using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public class IntentEnqueueSystem(World world, ILogger<IntentEnqueueSystem> logger) : BaseSystem<World, float>(world), IIntentHandler
{
    // internal para acesso do DequeueSystem (ou make internal/protected e friend)
    internal readonly Queue<EnterGameIntent> EnterGameQueue = new();
    internal readonly Queue<ExitGameIntent> ExitGameQueue = new();
    internal readonly Queue<MoveIntent> MoveQueue = new();
    internal readonly Queue<TeleportIntent> TeleportQueue = new();
    internal readonly Queue<AttackIntent> AttackQueue = new();

    // Enqueue methods for intents from players
    public void EnqueueEnterGameIntent(in EnterGameIntent intent) => EnterGameQueue.Enqueue(intent);
    public void EnqueueExitGameIntent(in ExitGameIntent intent) => ExitGameQueue.Enqueue(intent);
    public void EnqueueMoveIntent(in MoveIntent intent)  => MoveQueue.Enqueue(intent);
    public void EnqueueTeleportIntent(in TeleportIntent intent) => TeleportQueue.Enqueue(intent);
    public void EnqueueAttackIntent(in AttackIntent intent) => AttackQueue.Enqueue(intent);
}