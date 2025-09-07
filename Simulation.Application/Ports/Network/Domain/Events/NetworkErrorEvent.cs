namespace Simulation.Application.Ports.Network.Domain.Events;

/// <summary>
/// Evento disparado quando ocorre um erro de rede
/// Evento de domínio para notificar quando ocorre um erro de rede relevante para a aplicação.
/// Permite logging, métricas, alertas ou lógica de recuperação desacoplada da infraestrutura.
/// Pode ser usado para monitoramento e auditoria de falhas de comunicação.
/// </summary>
public class NetworkErrorEvent(string errorMessage, int? peerId = null)
{
    public string ErrorMessage { get; } = errorMessage;
    public int? PeerId { get; } = peerId;
}