namespace Simulation.Application.Ports.Network.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando ocorre um erro no processamento de pacotes
/// </summary>
public class PacketHandlingException : NetworkException
{
    public PacketHandlingException(string message) : base(message) { }
    public PacketHandlingException(string message, Exception innerException) : base(message, innerException) { }
}