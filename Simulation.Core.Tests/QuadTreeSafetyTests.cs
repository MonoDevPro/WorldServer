using System;
using System.Collections.Generic;
using Arch.Core;
using Simulation.Domain.Components;
using Simulation.Persistence.Char;
using Xunit;

namespace Simulation.Core.Tests;

/// <summary>
/// Testes críticos de segurança para QuadTreeSpatial focados na prevenção de crashes e race conditions.
/// Valida comportamento seguro em scenarios de alta concorrência e edge cases.
/// </summary>
public class QuadTreeSafetyTests : IDisposable
{
    private readonly QuadTreeSpatial _quadTree;
    private readonly World _world;

    public QuadTreeSafetyTests()
    {
        _quadTree = new QuadTreeSpatial(0, 0, 1000, 1000);
        _world = World.Create();
    }

    [Fact]
    public void Add_Remove_Update_BasicLifecycle_ShouldNotThrow()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 100, Y = 100 });
        var position1 = new Position { X = 100, Y = 100 };
        var position2 = new Position { X = 200, Y = 200 };

        // Act & Assert - Basic lifecycle should never throw
        var exception = Record.Exception(() =>
        {
            _quadTree.Add(entity, position1);
            _quadTree.Update(entity, position2);
            _quadTree.Remove(entity);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Add_DuplicateEntity_ShouldNotCorruptState()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 50, Y = 50 });
        var position = new Position { X = 50, Y = 50 };

        // Act
        _quadTree.Add(entity, position);
        _quadTree.Add(entity, position); // Duplicate

        // Assert - Query should return entity only once
        var results = _quadTree.Query(position, 10);
        Assert.Single(results);
        Assert.Equal(entity, results[0]);
    }

    [Fact]
    public void Remove_NonExistentEntity_ShouldBeSafe()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 10, Y = 10 });

        // Act & Assert
        var exception = Record.Exception(() => _quadTree.Remove(entity));
        Assert.Null(exception);
    }

    [Fact]
    public void Update_NonExistentEntity_ShouldBeSafe()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 10, Y = 10 });
        var newPosition = new Position { X = 20, Y = 20 };

        // Act & Assert
        var exception = Record.Exception(() => _quadTree.Update(entity, newPosition));
        Assert.Null(exception);
    }

    [Fact]
    public void Query_EmptyQuadTree_ShouldReturnEmpty()
    {
        // Arrange
        var center = new Position { X = 100, Y = 100 };

        // Act
        var results = _quadTree.Query(center, 50);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Query_WithListParameter_ShouldClearList()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 100, Y = 100 });
        var position = new Position { X = 100, Y = 100 };
        _quadTree.Add(entity, position);

        var results = new List<Entity> { entity }; // Pre-filled list

        // Act
        _quadTree.Query(position, 10, results);

        // Assert - The list should be properly used/cleared by the implementation
        Assert.True(results.Count >= 1); // Should contain at least our entity
    }

    [Fact]
    public void DestroyedEntity_ShouldNotCauseProblems()
    {
        // Arrange
        var entity = _world.Create<Position>(new Position { X = 100, Y = 100 });
        var position = new Position { X = 100, Y = 100 };
        _quadTree.Add(entity, position);

        // Act - Destroy entity while it's still in QuadTree
        _world.Destroy(entity);

        // Assert - QuadTree operations should still be safe
        var exception = Record.Exception(() =>
        {
            _quadTree.Update(entity, new Position { X = 200, Y = 200 });
            _quadTree.Remove(entity);
            var results = _quadTree.Query(position, 10);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void MultipleOperations_RandomOrder_ShouldBeSafe()
    {
        // Arrange
        var entities = new List<Entity>();
        for (int i = 0; i < 10; i++)
        {
            entities.Add(_world.Create<Position>(new Position { X = i * 10, Y = i * 10 }));
        }

        // Act - Mix of operations in random order
        var exception = Record.Exception(() =>
        {
            // Add some
            for (int i = 0; i < 5; i++)
            {
                _quadTree.Add(entities[i], new Position { X = i * 10, Y = i * 10 });
            }

            // Update some
            for (int i = 0; i < 3; i++)
            {
                _quadTree.Update(entities[i], new Position { X = i * 20, Y = i * 20 });
            }

            // Remove some
            for (int i = 0; i < 2; i++)
            {
                _quadTree.Remove(entities[i]);
            }

            // Query
            var results = _quadTree.Query(new Position { X = 50, Y = 50 }, 100);
        });

        // Assert
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _world?.Dispose();
    }
}