namespace Simulation.Application.Ports.Network.Domain.Events;

public readonly record struct ConnectionLatencyEvent(int PeerId, int Latency)
{
    // Evento de domínio para notificar quando a latência de uma conexão é estabelecida com sucesso.
    // Usado para acionar handlers de lógica de negócio, persistência ou métricas.
    // Mantém o domínio desacoplado da infraestrutura de rede.
}