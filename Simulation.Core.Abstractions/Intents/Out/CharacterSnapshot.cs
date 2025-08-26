using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Enums;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct CharacterSnapshot(CharId CharId, CharInfo Info, CharState State) : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId.CharacterId);
        writer.Put(Info.Name);
        writer.Put((byte)Info.Gender);
        writer.Put((byte)Info.Vocation);
        writer.Put(State.Position.X);
        writer.Put(State.Position.Y);
        writer.Put(State.Direction.X);
        writer.Put(State.Direction.Y);
        writer.Put(State.Speed);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = new CharId
        {
            CharacterId = reader.GetInt()
        };
        Info = new CharInfo
        {
            Name = reader.GetString(),
            Gender = (Gender)reader.GetByte(),
            Vocation = (Vocation)reader.GetByte()
        };
        State = new CharState
        {
            Position = new GameVector2(reader.GetInt(), reader.GetInt()),
            Direction = new GameVector2(reader.GetInt(), reader.GetInt()),
            Speed = reader.GetFloat()
        };
    }
}
    
    
