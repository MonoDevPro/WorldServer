namespace Simulation.Application.Ports.Network.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando ocorre um erro de serialização ou desserialização
/// </summary>
public class SerializationException : NetworkException
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception innerException) : base(message, innerException) { }
}