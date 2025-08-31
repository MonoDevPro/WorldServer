using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports;

namespace Simulation.Application.Systems;

public partial class SnapshotPublisherSystem : BaseSystem<World, float>, ISnapshotPublisher
{
    private readonly ISnapshotPublisher _publisher;
    private readonly ILogger<SnapshotPublisherSystem> _logger;

    [Event(order: 0)]
    public void Publish(in EnterSnapshot s) => _publisher.Publish(in s);

    [Event(order: 0)]
    public void Publish(in CharSnapshot snapshot) => _publisher.Publish(in snapshot);

    [Event(order: 0)]
    public void Publish(in ExitSnapshot s) => _publisher.Publish(in s);

    [Event(order: 0)]
    public void Publish(in MoveSnapshot s) => _publisher.Publish(in s);

    [Event(order: 0)]
    public void Publish(in AttackSnapshot s) => _publisher.Publish(in s);

    [Event(order: 0)]
    public void Publish(in TeleportSnapshot s) => _publisher.Publish(in s);

    public SnapshotPublisherSystem(World world, ISnapshotPublisher publisher, ILogger<SnapshotPublisherSystem> logger) 
        : base(world)
    {
        _publisher = publisher;
        _logger = logger;
        Hook();
    }
    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }
}