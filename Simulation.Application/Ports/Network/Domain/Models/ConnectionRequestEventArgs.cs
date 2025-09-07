namespace Simulation.Application.Ports.Network.Domain.Models;

/// <summary>
/// Argumentos para um evento de requisição de conexão
/// </summary>
public class ConnectionRequestEventArgs(ConnectionRequestInfo requestInfo)
{
    public ConnectionRequestInfo RequestInfo { get; } = requestInfo;
    public bool ShouldAccept { get; set; } = true;
}