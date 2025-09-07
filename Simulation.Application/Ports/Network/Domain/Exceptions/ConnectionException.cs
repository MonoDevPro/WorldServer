namespace Simulation.Application.Ports.Network.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando ocorre um erro de conexão
/// </summary>
public class ConnectionException : NetworkException
{
    public ConnectionException(string message) : base(message) { }
    public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
}