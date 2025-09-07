using Simulation.Application.Ports.Network.Outbound;

namespace Simulation.Application.Ports.Network.Domain.Models
{
    /// <summary>
    /// Representa um pacote de rede no domínio.
    /// Interface de marcação que todos os pacotes devem implementar.
    /// </summary>
    public interface IPacket {}
    
    /// <summary>
    /// Interface para objetos que podem ser serializados e desserializados
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializa o objeto para um escritor de rede
        /// </summary>
        /// <param name="writer">O escritor de rede</param>
        void Serialize(INetworkWriter writer);
        
        /// <summary>
        /// Desserializa o objeto de um leitor de rede
        /// </summary>
        /// <param name="reader">O leitor de rede</param>
        void Deserialize(INetworkReader reader);
    }
}