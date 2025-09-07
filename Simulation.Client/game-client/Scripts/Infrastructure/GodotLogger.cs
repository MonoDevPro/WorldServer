using Microsoft.Extensions.Logging;
using Godot;

namespace GameClient.Scripts.Infrastructure;

/// <summary>
/// Simple logger implementation for Godot that outputs to the console
/// </summary>
public class GodotLogger<T> : ILogger<T>
{
    private readonly string _categoryName = typeof(T).Name;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logMessage = $"[{logLevel}] {_categoryName}: {message}";
        
        if (exception != null)
        {
            logMessage += $"\n{exception}";
        }
        
        switch (logLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                GD.PrintErr(logMessage);
                break;
            case LogLevel.Warning:
                GD.Print($"WARNING: {logMessage}");
                break;
            case LogLevel.Information:
            case LogLevel.Debug:
            case LogLevel.Trace:
            default:
                GD.Print(logMessage);
                break;
        }
    }
}

/// <summary>
/// Simple logger factory for Godot
/// </summary>
public class GodotLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) { }

    public ILogger CreateLogger(string categoryName) => new GodotLogger<object>();

    public void Dispose() { }
}

/// <summary>
/// Extension class to easily register the Godot logger
/// </summary>
public static class GodotLoggerExtensions
{
    public static ILogger<T> CreateGodotLogger<T>() => new GodotLogger<T>();
}