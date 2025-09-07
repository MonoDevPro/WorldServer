using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Ports.ECS.Publishers;

namespace Simulation.Application.Services.ECS.Publishers;

public sealed partial class SnapshotForwarding : BaseSystem<World, float>, IPlayerSnapshotPublisher, IMapSnapshotPublisher
{
    private readonly IPlayerSnapshotPublisher _playerPublisher;
    private readonly IMapSnapshotPublisher? _mapPublisher;
    private readonly ILogger<SnapshotForwarding> _logger;
    
    [Event] public void Publish(in LoadMapSnapshot snapshot) => _mapPublisher?.Publish(in snapshot);
    [Event] public void Publish(in UnloadMapSnapshot snapshot) => _mapPublisher?.Publish(in snapshot);
    [Event] public void Publish(in JoinAckSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in PlayerJoinedSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in PlayerLeftSnapshot s) => _playerPublisher.Publish(s);
    [Event] public void Publish(in MoveSnapshot s) => _playerPublisher.Publish(in s);
    [Event] public void Publish(in AttackSnapshot s) => _playerPublisher.Publish(in s);
    [Event] public void Publish(in TeleportSnapshot s) => _playerPublisher.Publish(in s);

    public SnapshotForwarding(World world, IPlayerSnapshotPublisher playerPublisher, ILogger<SnapshotForwarding> logger, IMapSnapshotPublisher? mapPublisher = null) : base(world)
    {
        _playerPublisher = playerPublisher;
        _mapPublisher = mapPublisher;
        _logger = logger;
        Hook();
    }
    public override void Dispose()
    {
        Unhook();
        _playerPublisher.Dispose();
        _mapPublisher?.Dispose();
        base.Dispose();
    }
}