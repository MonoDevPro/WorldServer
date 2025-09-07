using Simulation.Application.Ports.Network.Domain.Models;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Domain.Components;

namespace Simulation.Networking.Packets;

// C -> S
    public class EnterIntentPacket : IPacket, ISerializable
    {
        public int CharId { get; set; }

        public void Serialize(INetworkWriter writer) => writer.WriteInt(CharId);
        public void Deserialize(INetworkReader reader) => CharId = reader.ReadInt();
    }

    // C -> S
    public class ExitIntentPacket : IPacket, ISerializable
    {
        public int CharId { get; set; }

        public void Serialize(INetworkWriter writer) => writer.WriteInt(CharId);
        public void Deserialize(INetworkReader reader) => CharId = reader.ReadInt();
    }

    // C -> S
    public class AttackIntentPacket : IPacket, ISerializable
    {
        public int CharId { get; set; }

        public void Serialize(INetworkWriter writer) => writer.WriteInt(CharId);
        public void Deserialize(INetworkReader reader) => CharId = reader.ReadInt();
    }

    // C -> S
    public class MoveIntentPacket : IPacket, ISerializable
    {
        public int CharId { get; set; }
        public Input Input { get; set; } // Supondo que 'Input' seja serializável

        public void Serialize(INetworkWriter writer)
        {
            writer.WriteInt(CharId);
            // Assumindo que Input tenha X e Y, por exemplo.
            // Se Input for mais complexo, ele também deve implementar ISerializable.
            writer.WriteInt(Input.X); 
            writer.WriteInt(Input.Y);
        }

        public void Deserialize(INetworkReader reader)
        {
            CharId = reader.ReadInt();
            Input = new Input { X = reader.ReadInt(), Y = reader.ReadInt() };
        }
    }

    // C -> S
    public class TeleportIntentPacket : IPacket, ISerializable
    {
        public int CharId { get; set; }
        public int MapId { get; set; }
        public Position Pos { get; set; }

        public void Serialize(INetworkWriter writer)
        {
            writer.WriteInt(CharId);
            writer.WriteInt(MapId);
            writer.WriteInt(Pos.X);
            writer.WriteInt(Pos.Y);
        }

        public void Deserialize(INetworkReader reader)
        {
            CharId = reader.ReadInt();
            MapId = reader.ReadInt();
            Pos = new Position { X = reader.ReadInt(), Y = reader.ReadInt() };
        }
    }