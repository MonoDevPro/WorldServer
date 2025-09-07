using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Network.Inbound;

namespace Simulation.Networking.Services;

/// <summary>
/// Implementação em memória do barramento de eventos de rede.
///
/// <para>
/// <b>Aviso de Uso:</b> Este barramento é indicado para eventos administrativos, de controle, conexão/desconexão e erros de rede,
/// onde a performance não é crítica. <b>Não utilize este barramento para eventos de gameplay, processamento de pacotes em alta frequência
/// ou qualquer fluxo que exija latência mínima e throughput máximo</b>, como movimentação de jogadores, ações em tempo real, etc.
/// Para esses casos, utilize mecanismos diretos e otimizados, evitando overhead de abstração e casting.
/// </para>
/// </summary>
public class NetworkEventBus(ILogger<NetworkEventBus> logger) : INetworkEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();

    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            logger.LogTrace("Não há manipuladores registrados para o evento do tipo {EventType}", eventType.Name);
            return;
        }
        
        foreach (var handler in handlers)
        {
            try
            {
                ((Action<TEvent>)handler)(eventData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao executar manipulador de evento para {EventType}", eventType.Name);
            }
        }
    }
    
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<object>();
            _handlers[eventType] = handlers;
        }
        
        handlers.Add(handler);
        logger.LogDebug("Manipulador inscrito para eventos do tipo {EventType}", eventType.Name);
    }
    
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return;
        }
        
        handlers.Remove(handler);
        logger.LogDebug("Manipulador removido de eventos do tipo {EventType}", eventType.Name);
    }
}