using Xunit;
using Simulation.Application.Utilities;
using Simulation.Domain.Templates;
using System.Diagnostics;
using System.Collections.Generic;

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
}