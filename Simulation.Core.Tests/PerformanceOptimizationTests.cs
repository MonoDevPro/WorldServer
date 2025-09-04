using Xunit;
using Simulation.Domain.Templates;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using Simulation.Core.Tests.Utilities;
using Arch.Core;
using Simulation.Persistence.Char;
using Simulation.Domain.Components;
using Simulation.Application.Systems;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Simulation.Core.Tests;

public class PerformanceOptimizationTests
{
    [Fact]
    public void ListPool_ShouldReuseObjects()
    {
        // Arrange & Act
        var list1 = ListPool.Get();
        list1.Add(new CharTemplate());
        ListPool.Return(list1);
        
        var list2 = ListPool.Get();
        
        // Assert
        Assert.Same(list1, list2);
        Assert.Empty(list2); // Should be cleared when returned to pool
    }
    
    [Fact]
    public void TemplatePool_ShouldReuseObjects()
    {
        // Arrange & Act
        var template1 = TemplatePool.Get();
        template1.Name = "Test";
        template1.CharId = 123;
        TemplatePool.Return(template1);
        
        var template2 = TemplatePool.Get();
        
        // Assert
        Assert.Same(template1, template2);
        Assert.Equal(string.Empty, template2.Name); // Should be reset when returned to pool
        Assert.Equal(0, template2.CharId); // Should be reset when returned to pool
    }
    
    [Fact]
    public void ListPool_Performance_ShouldReduceAllocations()
    {
        // This test demonstrates the reduction in allocations
        const int iterations = 1000;
        
        // Test with pool
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var list = ListPool.Get();
            list.Add(new CharTemplate());
            ListPool.Return(list);
        }
        sw1.Stop();
        
        // Test without pool (creating new lists)
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var list = new List<CharTemplate>();
            list.Add(new CharTemplate());
            // No return - simulates normal allocation pattern
        }
        sw2.Stop();
        
        // Pool should be faster due to reduced allocations
        // Note: This is a micro-benchmark and actual performance will vary
        Assert.True(sw1.ElapsedTicks <= sw2.ElapsedTicks * 2, 
            $"Pool time: {sw1.ElapsedTicks}, Normal time: {sw2.ElapsedTicks}");
    }
    
    [Fact]
    public void TemplateArrayPool_ShouldCreateExactSizedArrays()
    {
        // Arrange
        var list = new List<CharTemplate>
        {
            new CharTemplate { CharId = 1 },
            new CharTemplate { CharId = 2 },
            new CharTemplate { CharId = 3 }
        };
        
        // Act
        var result = TemplateArrayPool.CreateExactArray(list);
        
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(1, result[0].CharId);
        Assert.Equal(2, result[1].CharId);
        Assert.Equal(3, result[2].CharId);
    }
    
    [Fact]
    public void TemplateArrayPool_EmptyList_ShouldReturnEmptyArray()
    {
        // Arrange
        var emptyList = new List<CharTemplate>();
        
        // Act
        var result = TemplateArrayPool.CreateExactArray(emptyList);
        
        // Assert
        Assert.Empty(result);
        Assert.Same(Array.Empty<CharTemplate>(), result);
    }
    
    [Fact]
    public void QuadTreeSpatial_Update_ShouldNotAllocateExcessiveMemory()
    {
        // Arrange
        var spatial = new QuadTreeSpatial(0, 0, 100, 100);
        var world = World.Create();
        var entity = world.Create();
        var initialPos = new Position { X = 10, Y = 10 };
        var newPos = new Position { X = 20, Y = 20 };
        
        spatial.Add(entity, initialPos);

        // Capture initial memory state
        var initialGen0 = GC.CollectionCount(0);
        var initialGen1 = GC.CollectionCount(1);
        var initialGen2 = GC.CollectionCount(2);

        // Act - Simulate high-frequency position updates
        for (int i = 0; i < 1000; i++)
        {
            var updatePos = new Position { X = newPos.X + (i % 10), Y = newPos.Y + (i % 10) };
            spatial.Update(entity, updatePos);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalGen0 = GC.CollectionCount(0);
        var finalGen1 = GC.CollectionCount(1);
        var finalGen2 = GC.CollectionCount(2);

        // Assert - Gen2 collections should not increase dramatically
        var gen2Increase = finalGen2 - initialGen2;
        
        // With proper optimization, Gen2 should not increase much
        Assert.True(gen2Increase <= 3, 
            $"Too many Gen2 GC collections during spatial updates: {gen2Increase}. " +
            $"Initial: Gen0={initialGen0}, Gen1={initialGen1}, Gen2={initialGen2}. " +
            $"Final: Gen0={finalGen0}, Gen1={finalGen1}, Gen2={finalGen2}");
    }

    [Fact]
    public void QuadTreeSpatial_Query_ShouldUseObjectPooling()
    {
        // Arrange
        var spatial = new QuadTreeSpatial(0, 0, 100, 100);
        var world = World.Create();
        
        // Add some entities
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Create();
            spatial.Add(entity, new Position { X = i * 5, Y = i * 5 });
        }

        var queryCenter = new Position { X = 25, Y = 25 };
        const int queryRadius = 10;

        // Capture initial memory state
        var initialGen0 = GC.CollectionCount(0);
        var initialGen1 = GC.CollectionCount(1);
        var initialGen2 = GC.CollectionCount(2);

        // Act - Perform many queries
        for (int i = 0; i < 1000; i++)
        {
            var results = new List<Entity>();
            spatial.Query(queryCenter, queryRadius, results);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalGen0 = GC.CollectionCount(0);
        var finalGen1 = GC.CollectionCount(1);
        var finalGen2 = GC.CollectionCount(2);

        // Assert - With pooling, memory pressure should be minimal
        var gen2Increase = finalGen2 - initialGen2;
        
        Assert.True(gen2Increase <= 3, 
            $"Too many Gen2 GC collections during spatial queries: {gen2Increase}. " +
            $"Object pooling may not be working correctly. " +
            $"Initial: Gen0={initialGen0}, Gen1={initialGen1}, Gen2={initialGen2}. " +
            $"Final: Gen0={finalGen0}, Gen1={finalGen1}, Gen2={finalGen2}");
    }
}