using NUnit.Framework;
using Arch.Core;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core.Tests.Systems;

[TestFixture]
public class TeleportSystemTests
{
    private World _world;
    private TeleportSystem _system;
    private BlockingIndex _blockingIndex;
    private BoundsIndex _boundsIndex;

    [SetUp]
    public void Setup()
    {
        _world = World.Create();
        _blockingIndex = new BlockingIndex();
        _boundsIndex = new BoundsIndex();

        // Configura um mapa com limites definidos (mapId = 1)
        _world.Create(new Bounds { MinX = 0, MinY = 0, MaxX = 100, MaxY = 100 }, new MapRef { MapId = 1 });
        _boundsIndex.RebuildIfDirty(_world);
        
        // Configura um tile bloqueado no mapa
        _world.Create(new Blocking(), new TilePosition { Position = new GameVector2(10, 10) }, new MapRef { MapId = 1 });
        _blockingIndex.RebuildIfDirty(_world);

        _system = new TeleportSystem(_world, _blockingIndex, _boundsIndex);
    }

    [TearDown]
    public void Teardown()
    {
        World.Destroy(_world);
    }

    [Test]
    public void Apply_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var entity = _world.Create(new TilePosition { Position = new GameVector2(0, 0) }, new MapRef { MapId = 1 });
        var targetPosition = new TilePosition { Position = new GameVector2(25, 50) };
        var cmd = new Requests.Teleport(entity, 1, targetPosition);

        // Act
        var result = _system.Apply(in cmd);

        // Assert
        Assert.That(result, Is.True);
        var newPos = _world.Get<TilePosition>(entity);
        var newMap = _world.Get<MapRef>(entity);
        
        Assert.That(newPos.Position, Is.EqualTo(targetPosition.Position));
        Assert.That(newMap.MapId, Is.EqualTo(1));
    }

    [Test]
    public void Apply_ToNonExistentEntity_ShouldFail()
    {
        // Arrange
        var deadEntity = _world.Create();
        _world.Destroy(deadEntity);
        var targetPosition = new TilePosition { Position = new GameVector2(5, 5) };
        var cmd = new Requests.Teleport(deadEntity, 1, targetPosition);

        // Act
        var result = _system.Apply(in cmd);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Apply_ToBlockedTile_ShouldFail()
    {
        // Arrange
        var entity = _world.Create(new TilePosition { Position = new GameVector2(0, 0) }, new MapRef { MapId = 1 });
        var blockedPosition = new TilePosition { Position = new GameVector2(10, 10) }; // Posição bloqueada no Setup
        var cmd = new Requests.Teleport(entity, 1, blockedPosition);
        
        // Act
        var result = _system.Apply(in cmd);

        // Assert
        Assert.That(result, Is.False);
        // Garante que a entidade não se moveu
        var currentPos = _world.Get<TilePosition>(entity);
        Assert.That(currentPos.Position, Is.EqualTo(new GameVector2(0, 0)));
    }

    [Test]
    public void Apply_ToOutOfBoundsTile_ShouldFail()
    {
        // Arrange
        var entity = _world.Create(new TilePosition { Position = new GameVector2(0, 0) }, new MapRef { MapId = 1 });
        var outOfBoundsPosition = new TilePosition { Position = new GameVector2(101, 101) }; // Fora dos limites definidos no Setup
        var cmd = new Requests.Teleport(entity, 1, outOfBoundsPosition);

        // Act
        var result = _system.Apply(in cmd);

        // Assert
        Assert.That(result, Is.False);
        var currentPos = _world.Get<TilePosition>(entity);
        Assert.That(currentPos.Position, Is.EqualTo(new GameVector2(0, 0)));
    }
    
    [Test]
    public void Apply_ToDifferentMap_ShouldUpdateMapRef()
    {
        // Arrange
        // Adiciona um segundo mapa
        _world.Create(new Bounds { MinX = 0, MinY = 0, MaxX = 50, MaxY = 50 }, new MapRef { MapId = 2 });
        _boundsIndex.RebuildIfDirty(_world);
        
        var entity = _world.Create(new TilePosition { Position = new GameVector2(0, 0) }, new MapRef { MapId = 1 });
        var targetPosition = new TilePosition { Position = new GameVector2(5, 5) };
        var cmd = new Requests.Teleport(entity, 2, targetPosition); // Teleportando para o mapa 2

        // Act
        var result = _system.Apply(in cmd);

        // Assert
        Assert.That(result, Is.True);
        var newMap = _world.Get<MapRef>(entity);
        var newPos = _world.Get<TilePosition>(entity);

        Assert.That(newMap.MapId, Is.EqualTo(2));
        Assert.That(newPos.Position, Is.EqualTo(targetPosition.Position));
    }
}