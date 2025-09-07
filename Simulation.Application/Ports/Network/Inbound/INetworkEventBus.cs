namespace Simulation.Application.Ports.Network.Inbound;

/// <summary>
/// Interface para o barramento de eventos de rede
/// </summary>
public interface INetworkEventBus
{
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}