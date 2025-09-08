using Arch.Bus;
using Arch.Core;
using Arch.System;
using Simulation.Application.Ports.ECS.Publishers;
using Simulation.Domain;

namespace Simulation.ECS.Systems.Publishers;

public sealed partial class SnapshotForwarding : BaseSystem<World, float>, IPlayerSnapshotPublisher
{
    private readonly IPlayerSnapshotPublisher _playerPublisher;
    
    [Event] public void Publish(in JoinAckSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in PlayerJoinedSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in PlayerLeftSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in MoveSnapshot s) => _playerPublisher.Publish(in s);
    [Event] public void Publish(in AttackSnapshot s) => _playerPublisher.Publish(in s);
    [Event] public void Publish(in TeleportSnapshot s) => _playerPublisher.Publish(in s);

    public SnapshotForwarding(World world, IPlayerSnapshotPublisher playerPublisher) : base(world)
    {
        _playerPublisher = playerPublisher;
        Hook();
    }
    public override void Dispose()
    {
        Unhook();
        _playerPublisher.Dispose();
        base.Dispose();
    }
}