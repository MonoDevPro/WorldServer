using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;

namespace Simulation.Core.Systems;

public class IntentsEnqueueSystem(World world) : BaseSystem<World, float>(world), IIntentProducer
{
    // internal para acesso do DequeueSystem (ou make internal/protected e friend)
    internal readonly ConcurrentQueue<MoveIntent> MoveQueue = new();
    internal readonly ConcurrentQueue<TeleportIntent> TeleportQueue = new();
    internal readonly ConcurrentQueue<AttackIntent> AttackQueue = new();
    // Novas filas para o ciclo de vida do jogador.
    internal readonly ConcurrentQueue<EnterGameIntent> EnterGameQueue = new();
    internal readonly ConcurrentQueue<ExitGameIntent> ExitGameQueue = new();

    public void EnqueueMoveIntent(in MoveIntent intent)  => MoveQueue.Enqueue(intent);
    public void EnqueueTeleportIntent(in TeleportIntent intent) => TeleportQueue.Enqueue(intent);
    public void EnqueueAttackIntent(in AttackIntent intent) => AttackQueue.Enqueue(intent);
    // Implementação dos novos métodos da interface.
    public void EnqueueEnterGameIntent(in EnterGameIntent intent) => EnterGameQueue.Enqueue(intent);
    public void EnqueueExitGameIntent(in ExitGameIntent intent) => ExitGameQueue.Enqueue(intent);
}