using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Simulation.Application.Services;
using Simulation.Application.Services.Loop;
using Xunit;

namespace Simulation.Core.Tests;

/// <summary>
/// Testes básicos para PerformanceMonitor que validam funcionalidade essencial
/// e comportamento seguro em cenários normais e edge cases.
/// </summary>
public class PerformanceMonitorTests : IDisposable
{
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly Mock<ILogger<PerformanceMonitor>> _mockLogger;

    public PerformanceMonitorTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceMonitor>>();
        _performanceMonitor = new PerformanceMonitor(_mockLogger.Object);
    }

    [Fact]
    public void RecordTick_NormalDuration_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => _performanceMonitor.RecordTick(16.67));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordTick_SlowTick_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => _performanceMonitor.RecordTick(25.0));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordTick_VerySlowTick_ShouldLogWarning()
    {
        // Arrange & Act
        _performanceMonitor.RecordTick(60.0); // Very slow tick

        // Assert - Should log but not throw
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tick muito lento")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void RecordTick_ZeroDuration_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => _performanceMonitor.RecordTick(0.0));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordTick_NegativeDuration_ShouldNotThrow()
    {
        // Arrange & Act & Assert - Defensive programming
        var exception = Record.Exception(() => _performanceMonitor.RecordTick(-1.0));
        Assert.Null(exception);
    }

    [Fact]
    public void GetCurrentStats_AfterMultipleTicks_ShouldReturnCorrectData()
    {
        // Arrange
        _performanceMonitor.RecordTick(10.0);
        _performanceMonitor.RecordTick(20.0);
        _performanceMonitor.RecordTick(30.0);

        // Act
        var stats = _performanceMonitor.GetCurrentStats();

        // Assert
        Assert.Equal(3, stats.TotalTicks);
        Assert.Equal(20.0, stats.AverageTickTimeMs); // (10+20+30)/3 = 20
        Assert.True(stats.TotalMemoryMB > 0);
        Assert.True(stats.Gen0Collections >= 0);
    }

    [Fact]
    public void GetCurrentStats_NoTicks_ShouldReturnZeroAverage()
    {
        // Act
        var stats = _performanceMonitor.GetCurrentStats();

        // Assert
        Assert.Equal(0, stats.TotalTicks);
        Assert.Equal(0.0, stats.AverageTickTimeMs);
    }

    [Fact]
    public void GetCurrentStats_ConcurrentAccess_ShouldNotThrow()
    {
        // Arrange & Act - Simulate concurrent access
        var exception = Record.Exception(() =>
        {
            // Simulate multiple threads
            Parallel.For(0, 10, i =>
            {
                _performanceMonitor.RecordTick(15.0 + i);
                var stats = _performanceMonitor.GetCurrentStats();
            });
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _performanceMonitor.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void DoubleDispose_ShouldBeSafe()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
        {
            _performanceMonitor.Dispose();
            _performanceMonitor.Dispose(); // Double dispose
        });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        _performanceMonitor?.Dispose();
    }
}