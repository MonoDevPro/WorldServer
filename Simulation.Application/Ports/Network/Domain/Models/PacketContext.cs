namespace Simulation.Application.Ports.Network.Domain.Models;

    /// <summary>
    /// Contexto de processamento de um pacote
    /// </summary>
    public class PacketContext
    {
        public int PeerId { get; }
        public byte Channel { get; }
        
        public PacketContext(int peerId, byte channel = 0)
        {
            PeerId = peerId;
            Channel = channel;
        }
    }