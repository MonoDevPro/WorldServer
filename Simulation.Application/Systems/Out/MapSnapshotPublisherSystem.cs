using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Map;

namespace Simulation.Application.Systems.Out;

public partial class MapSnapshotPublisherSystem : BaseSystem<World, float>, IMapSnapshotPublisher
{
    private readonly IMapSnapshotPublisher _publisher;
    private readonly ILogger<MapSnapshotPublisherSystem> _logger;
    
    [Event(order: 0)]
    public void Publish(in LoadMapSnapshot snapshot) => _publisher.Publish(in snapshot);
    
    [Event(order: 0)]
    public void Publish(in UnloadMapSnapshot snapshot) => _publisher.Publish(in snapshot);

    
    public MapSnapshotPublisherSystem(World world, IMapSnapshotPublisher publisher, ILogger<MapSnapshotPublisherSystem> logger) 
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