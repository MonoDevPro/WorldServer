using System;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Application.Systems;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;
using Simulation.Factories;
using Simulation.Pooling;
using Xunit;

namespace Simulation.Core.Tests;

/// <summary>
/// Testes para verificar se o gerenciamento de memória está correto,
/// especialmente o uso de pools de objetos e arrays.
/// </summary>
public class MemoryManagementTests
{
    [Fact]
    public void ArrayPool_ShouldBeReusable_AfterReturnArray()
    {
        // Arrange
        var arrayPool = new DefaultArrayPoolAdapter<CharTemplate>();

        // Act & Assert - Rent and return arrays multiple times
        for (int i = 0; i < 10; i++)
        {
            var array1 = arrayPool.Rent(5);
            Assert.NotNull(array1);
            Assert.True(array1.Length >= 5);
            
            // Fill with test data
            for (int j = 0; j < Math.Min(5, array1.Length); j++)
            {
                array1[j] = new CharTemplate { CharId = j, Name = $"Test{j}" };
            }
            
            arrayPool.Return(array1);
            
            // Rent again - might get the same array back due to pooling
            var array2 = arrayPool.Rent(5);
            Assert.NotNull(array2);
            Assert.True(array2.Length >= 5);
            
            // Array should be cleared after return (clearArray: true in DefaultArrayPoolAdapter)
            for (int j = 0; j < Math.Min(5, array2.Length); j++)
            {
                // The array pool clears the array, so elements should be null or default
                if (array2[j] != null)
                {
                    Assert.Equal(0, array2[j].CharId);
                    Assert.True(string.IsNullOrEmpty(array2[j].Name));
                }
                // If null, that's also a valid cleared state
            }
            
            arrayPool.Return(array2);
        }
    }

    [Fact]
    public void CharTemplatePool_ShouldResetObjects_WhenReturned()
    {
        // Arrange
        var pool = new MicrosoftObjectPoolAdapter<CharTemplate>(
            new DefaultObjectPool<CharTemplate>(new CharTemplatePolicy()));

        // Act
        var template1 = pool.Get();
        template1.CharId = 123;
        template1.Name = "Test Character";
        template1.MapId = 456;
        
        pool.Return(template1);
        
        var template2 = pool.Get();

        // Assert - Object should be reset when returned to pool
        Assert.Equal(0, template2.CharId);
        Assert.Equal(string.Empty, template2.Name);
        Assert.Equal(0, template2.MapId);
        
        pool.Return(template2);
    }

    [Fact]
    public void MemoryPressure_ShouldNotIncrease_WithRepeatedOperations()
    {
        // Arrange
        var arrayPool = new DefaultArrayPoolAdapter<CharTemplate>();
        var templatePool = new MicrosoftObjectPoolAdapter<CharTemplate>(
            new DefaultObjectPool<CharTemplate>(new CharTemplatePolicy()));

        // Capture initial memory state
        var initialGen0 = GC.CollectionCount(0);
        var initialGen1 = GC.CollectionCount(1);
        var initialGen2 = GC.CollectionCount(2);

        // Act - Simulate high-frequency operations similar to the game server
        for (int i = 0; i < 100; i++)
        {
            // Rent array and templates
            var array = arrayPool.Rent(10);
            for (int j = 0; j < 10; j++)
            {
                var template = templatePool.Get();
                template.CharId = j;
                template.Name = $"Char{j}";
                array[j] = template;
                templatePool.Return(template);
            }
            
            // Return array
            arrayPool.Return(array);
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
        
        // With proper pooling, Gen2 should not increase much
        Assert.True(gen2Increase <= 2, 
            $"Gen2 GC collections increased by {gen2Increase}, indicating potential memory pressure. " +
            $"Initial: Gen0={initialGen0}, Gen1={initialGen1}, Gen2={initialGen2}. " +
            $"Final: Gen0={finalGen0}, Gen1={finalGen1}, Gen2={finalGen2}");
    }
}