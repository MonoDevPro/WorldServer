using Arch.Core;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Network.Inbound;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Application.Options;

namespace GameClient.Scripts.Infrastructure;

/// <summary>
/// Simple service container for dependency injection in the Godot client
/// </summary>
public class ServiceContainer
{
    private readonly Dictionary<Type, object> _services = new();
    
    public void RegisterSingleton<T>(T instance) where T : class
    {
        _services[typeof(T)] = instance;
    }
    
    public T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service of type {typeof(T).Name} not registered");
    }
    
    public bool TryGet<T>(out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }
        service = null;
        return false;
    }
}