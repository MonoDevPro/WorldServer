using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Map;

namespace Simulation.Application.Services.Publishers;

public sealed partial class SnapshotForwarding : IDisposable
{
    private readonly ICharSnapshotPublisher _charPublisher;
    private readonly IMapSnapshotPublisher _mapPublisher;
    private readonly ILogger<SnapshotForwarding> _logger;
    
    [Event]
    public void Publish(in LoadMapSnapshot snapshot) => _mapPublisher.Publish(in snapshot);
    
    [Event]
    public void Publish(in UnloadMapSnapshot snapshot) => _mapPublisher.Publish(in snapshot);

    [Event]
    public void Publish(in EnterSnapshot s) => _charPublisher.Publish(in s);

    [Event]
    public void Publish(in CharSnapshot snapshot) => _charPublisher.Publish(in snapshot);

    [Event]
    public void Publish(in ExitSnapshot s) => _charPublisher.Publish(in s);

    [Event]
    public void Publish(in MoveSnapshot s) => _charPublisher.Publish(in s);

    [Event]
    public void Publish(in AttackSnapshot s) => _charPublisher.Publish(in s);

    [Event]
    public void Publish(in TeleportSnapshot s) => _charPublisher.Publish(in s);
    
    // Persistant data
    [Event]
    public void Publish(CharSaveTemplate s) => _charPublisher.Publish(in s);

    
    public SnapshotForwarding(World world, ICharSnapshotPublisher charPublisher, IMapSnapshotPublisher mapPublisher, ILogger<SnapshotForwarding> logger) 
    {
        _charPublisher = charPublisher;
        _mapPublisher = mapPublisher;
        _logger = logger;
        Hook();
    }
    public void Dispose()
    {
        Unhook();
    }
}