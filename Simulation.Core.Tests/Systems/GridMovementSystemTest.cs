using System;
using Arch.Core;
using NUnit.Framework;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

// <- Garanta que está usando o NUnit

namespace Simulation.Core.Tests.Systems;

[TestFixture]
public class GridMovementSystemTests
{
    private World _world;
    private GridMovementSystem _system;
    private BlockingIndex _blockingIndex;
    private BoundsIndex _boundsIndex;

    [SetUp]
    public void Setup()
    {
        _world = World.Create();
        _blockingIndex = new BlockingIndex();
        _boundsIndex = new BoundsIndex();

        // Adiciona um mapa válido com limites para os testes
        _world.Create(new Bounds { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 }, new MapRef { MapId = 1 });
        _boundsIndex.RebuildIfDirty(_world);

        _system = new GridMovementSystem(_world, _blockingIndex, _boundsIndex);
    }

    [TearDown]
    public void Teardown()
    {
        World.Destroy(_world);
    }

    private Entity CreateMovableEntity(GameVector2 initialPosition, int mapId = 1, float speed = 1.0f)
    {
        return _world.Create(
            new TilePosition { Position = initialPosition },
            new TileVelocity { Velocity = VelocityVector.Zero },
            new MoveSpeed { Value = speed },
            new MapRef { MapId = mapId }
        );
    }

    // --- Testes para o método Apply ---

    [Test]
    public void Apply_WithValidCommand_ShouldSucceedAndSetComponents()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(5, 5));
        var moveCmd = new Requests.Move(entity, 1, new DirectionInput { Direction = new VelocityVector(1, 0) });

        // Act
        var result = _system.Apply(in moveCmd);

        // Assert
        Assert.That(result, Is.True);
        var dir = _world.Get<DirectionInput>(entity);
        var map = _world.Get<MapRef>(entity);
        Assert.That(map.MapId, Is.EqualTo(1));
        Assert.That(dir.Direction, Is.EqualTo(moveCmd.Input.Direction));
    }

    [Test]
    public void Apply_ToNonExistentEntity_ShouldReturnFalse()
    {
        // Arrange
        var deadEntity = _world.Create();
        _world.Destroy(deadEntity);
        var moveCmd = new Requests.Move(deadEntity, 1, new DirectionInput { Direction = new VelocityVector(1, 0) });

        // Act
        var result = _system.Apply(in moveCmd);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Apply_ToEntityMissingComponents_ShouldThrowException()
    {
        // Arrange
        var entity = _world.Create(); // Entidade sem os componentes necessários
        var moveCmd = new Requests.Move(entity, 1, new DirectionInput { Direction = new VelocityVector(1, 0) });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _system.Apply(in moveCmd));
    }

    // --- Testes para a lógica de processamento (Queries) ---

    [Test]
    public void Update_ProcessDirectionInput_ShouldSetVelocityAndResetDirection()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(5, 5), speed: 2.0f);
        var moveCmd = new Requests.Move(entity, 1, new DirectionInput { Direction = new VelocityVector(0, 1) });
        _system.Apply(in moveCmd);

        // Act
        _system.Update(0.1f);

        // Assert
        var vel = _world.Get<TileVelocity>(entity);
        var dir = _world.Get<DirectionInput>(entity);

        Assert.That(vel.Velocity.Y, Is.EqualTo(2.0f));
        Assert.That(dir.IsZero, Is.True);
    }

    [Test]
    public void Update_ProcessMovement_ShouldMoveOneTile()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(5, 5), speed: 1.0f);
        _world.Set(entity, new TileVelocity { Velocity = new VelocityVector(1, 0) });

        // Act
        _system.Update(1.0f);

        // Assert
        var pos = _world.Get<TilePosition>(entity);
        Assert.That(pos.Position, Is.EqualTo(new GameVector2(6, 5)));
    }

    [Test]
    public void Update_ProcessMovement_WithAccumulatedDeltaTime_ShouldMove()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(5, 5), speed: 1.0f);
        _world.Set(entity, new TileVelocity { Velocity = new VelocityVector(1, 0) });

        // Act
        _system.Update(0.6f); // Acumula 0.6
        var pos1 = _world.Get<TilePosition>(entity);
        Assert.That(pos1.Position, Is.EqualTo(new GameVector2(5, 5)), "Não deve mover na primeira atualização");

        _system.Update(0.6f); // Acumula 1.2 no total, agora deve mover 1 tile
        var pos2 = _world.Get<TilePosition>(entity);
        Assert.That(pos2.Position, Is.EqualTo(new GameVector2(6, 5)), "Deve mover na segunda atualização");
    }

    [Test]
    public void Update_ProcessMovement_TowardsBlockedTile_ShouldNotMove()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(5, 5), speed: 1.0f);
        _world.Set(entity, new TileVelocity { Velocity = new VelocityVector(1, 0) });
        _world.Create(new Blocking(), new TilePosition { Position = new GameVector2(6, 5) }, new MapRef { MapId = 1 });
        _blockingIndex.MarkDirty(); // Marca o índice como sujo para forçar a reconstrução

        // Act
        _system.Update(1.0f);

        // Assert
        var pos = _world.Get<TilePosition>(entity);
        Assert.That(pos.Position, Is.EqualTo(new GameVector2(5, 5)));
    }

    [Test]
    public void Update_ProcessMovement_TowardsOutOfBoundsTile_ShouldNotMove()
    {
        // Arrange
        var entity = CreateMovableEntity(new GameVector2(10, 10), speed: 1.0f); // No limite do mapa
        _world.Set(entity, new TileVelocity { Velocity = new VelocityVector(1, 0) });

        // Act
        _system.Update(1.0f);

        // Assert
        var pos = _world.Get<TilePosition>(entity);
        Assert.That(pos.Position, Is.EqualTo(new GameVector2(10, 10)));
    }
}