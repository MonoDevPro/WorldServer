using System;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Simulation.Application.DTOs;
using Simulation.Client.Systems;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;
using Xunit;

namespace Simulation.Core.Tests;

/// <summary>
/// Testes críticos de segurança para lifecycle de entidades.
/// Foca na prevenção de dangling references e AccessViolations durante criação/destruição.
/// </summary>
public class EntityLifecycleSafetyTests : IDisposable
{
    private readonly World _world;
    private readonly SnapshotHandlerSystem _snapshotHandler;
    private readonly Mock<ILogger<SnapshotHandlerSystem>> _mockLogger;

    public EntityLifecycleSafetyTests()
    {
        _world = World.Create();
        _mockLogger = new Mock<ILogger<SnapshotHandlerSystem>>();
        _snapshotHandler = new SnapshotHandlerSystem(_world, _mockLogger.Object);
    }

    [Fact]
    public void HandleSnapshot_CharSnapshot_ShouldCreateEntitySafely()
    {
        // Arrange
        var charTemplate = new CharTemplate
        {
            CharId = 1,
            MapId = 1,
            Position = new Position { X = 10, Y = 10 },
            Direction = new Direction { X = 1, Y = 0 }
        };

        var charSnapshot = new CharSnapshot(1, 1, charTemplate);

        // Act
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(charSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        // Assert
        Assert.Null(exception);

        // Verify entity was created
        var entityCount = 0;
        _world.Query(new QueryDescription().WithAll<CharId>(), (Entity _) => entityCount++);
        Assert.Equal(1, entityCount);
    }

    [Fact]
    public void HandleSnapshot_ExitSnapshot_ShouldRemoveEntitySafely()
    {
        // Arrange - Create entity first
        var charTemplate = new CharTemplate
        {
            CharId = 2,
            MapId = 1,
            Position = new Position { X = 20, Y = 20 },
            Direction = new Direction { X = 0, Y = 1 }
        };

        var charSnapshot = new CharSnapshot(1, 2, charTemplate);
        _snapshotHandler.HandleSnapshot(charSnapshot);
        _snapshotHandler.Update(0.016f);

        // Act - Remove entity
        var exitSnapshot = new ExitSnapshot(1, 2, charTemplate);
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(exitSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        // Assert
        Assert.Null(exception);

        // Verify entity was removed
        var entityCount = 0;
        _world.Query(new QueryDescription().WithAll<CharId>(), (Entity _) => entityCount++);
        Assert.Equal(0, entityCount);
    }

    [Fact]
    public void HandleSnapshot_ExitSnapshot_NonExistentEntity_ShouldNotThrow()
    {
        // Arrange
        var charTemplate = new CharTemplate(); // Empty template
        var exitSnapshot = new ExitSnapshot(1, 999, charTemplate); // Non-existent CharId

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(exitSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void HandleSnapshot_DoubleExit_ShouldBeSafe()
    {
        // Arrange - Create entity
        var charTemplate = new CharTemplate
        {
            CharId = 3,
            MapId = 1,
            Position = new Position { X = 30, Y = 30 }
        };

        var charSnapshot = new CharSnapshot(1, 3, charTemplate);
        _snapshotHandler.HandleSnapshot(charSnapshot);
        _snapshotHandler.Update(0.016f);

        var exitSnapshot = new ExitSnapshot(1, 3, charTemplate);

        // Act & Assert - Exit twice should be safe
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(exitSnapshot);
            _snapshotHandler.Update(0.016f);
            
            // Second exit - should not crash
            _snapshotHandler.HandleSnapshot(exitSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void HandleSnapshot_MoveSnapshot_ShouldUpdatePositionSafely()
    {
        // Arrange - Create entity first
        var charTemplate = new CharTemplate
        {
            CharId = 4,
            MapId = 1,
            Position = new Position { X = 40, Y = 40 }
        };

        var charSnapshot = new CharSnapshot(1, 4, charTemplate);
        _snapshotHandler.HandleSnapshot(charSnapshot);
        _snapshotHandler.Update(0.016f);

        // Act - Move the entity
        var moveSnapshot = new MoveSnapshot(4, new Position { X = 40, Y = 40 }, new Position { X = 41, Y = 40 });
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(moveSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        // Assert
        Assert.Null(exception);

        // Verify position was updated
        var positionUpdated = false;
        _world.Query(new QueryDescription().WithAll<CharId, Position>(), (Entity entity, ref CharId charId, ref Position position) =>
        {
            if (charId.Value == 4)
            {
                positionUpdated = position.X == 41 && position.Y == 40;
            }
        });

        Assert.True(positionUpdated);
    }

    [Fact]
    public void ClearAllCharacters_ShouldRemoveAllEntitiesSafely()
    {
        // Arrange - Create multiple entities
        for (int i = 10; i < 15; i++)
        {
            var charTemplate = new CharTemplate
            {
                CharId = i,
                MapId = 1,
                Position = new Position { X = i, Y = i }
            };

            var charSnapshot = new CharSnapshot(1, i, charTemplate);
            _snapshotHandler.HandleSnapshot(charSnapshot);
        }
        _snapshotHandler.Update(0.016f);

        // Verify entities were created
        var entityCountBefore = 0;
        _world.Query(new QueryDescription().WithAll<CharId>(), (Entity _) => entityCountBefore++);
        Assert.Equal(5, entityCountBefore);

        // Act
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.ClearAllCharacters();
            _snapshotHandler.Update(0.016f);
        });

        // Assert
        Assert.Null(exception);

        // Verify all entities were removed
        var entityCountAfter = 0;
        _world.Query(new QueryDescription().WithAll<CharId>(), (Entity _) => entityCountAfter++);
        Assert.Equal(0, entityCountAfter);
    }

    [Fact]
    public void Update_ConcurrentSnapshots_ShouldProcessAllSafely()
    {
        // Arrange - Queue multiple snapshots without processing
        for (int i = 20; i < 25; i++)
        {
            var charTemplate = new CharTemplate
            {
                CharId = i,
                MapId = 1,
                Position = new Position { X = i, Y = i }
            };

            var charSnapshot = new CharSnapshot(1, i, charTemplate);
            _snapshotHandler.HandleSnapshot(charSnapshot);
        }

        // Act - Process all queued snapshots at once
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.Update(0.016f);
        });

        // Assert
        Assert.Null(exception);

        // Verify all entities were created
        var entityCount = 0;
        _world.Query(new QueryDescription().WithAll<CharId>(), (Entity _) => entityCount++);
        Assert.Equal(5, entityCount);
    }

    [Fact]
    public void HandleSnapshot_EmptyMapEntities_ShouldBeSafe()
    {
        // Arrange
        var enterSnapshot = new EnterSnapshot(1, 1, Array.Empty<CharTemplate>());

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            _snapshotHandler.HandleSnapshot(enterSnapshot);
            _snapshotHandler.Update(0.016f);
        });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        _world?.Dispose();
    }
}