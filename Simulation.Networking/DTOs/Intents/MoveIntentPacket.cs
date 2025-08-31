using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Domain.Components;

namespace Simulation.Networking.DTOs.Intents;

public struct MoveIntentPacket : INetSerializable
{
    public int CharId;
    public Input Input;

    public void FromDTO(in MoveIntent dto)
    {
        CharId = dto.CharId;
        Input = dto.Input;
    }

    public MoveIntent ToDTO() => new(CharId, Input);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(Input.X);
        writer.Put(Input.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        Input = new Input { X = reader.GetInt(), Y = reader.GetInt() };
    }
}