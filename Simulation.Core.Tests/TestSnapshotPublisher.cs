using System.Collections.Generic;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Tests;

internal sealed class TestSnapshotPublisher : ISnapshotPublisher
{
    public readonly List<EnterSnapshot> Enters = new();
    public readonly List<CharSnapshot> Chars = new();
    public readonly List<ExitSnapshot> Exits = new();
    public readonly List<MoveSnapshot> Moves = new();
    public readonly List<AttackSnapshot> Attacks = new();
    public readonly List<TeleportSnapshot> Teleports = new();

    public void Publish(in EnterSnapshot snapshot) => Enters.Add(snapshot);
    public void Publish(in CharSnapshot snapshot) => Chars.Add(snapshot);
    public void Publish(in ExitSnapshot snapshot) => Exits.Add(snapshot);
    public void Publish(in MoveSnapshot snapshot) => Moves.Add(snapshot);
    public void Publish(in AttackSnapshot snapshot) => Attacks.Add(snapshot);
    public void Publish(in TeleportSnapshot snapshot) => Teleports.Add(snapshot);

    public void Dispose() { }
}
