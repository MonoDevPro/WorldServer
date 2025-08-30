using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters;

public record struct EnterSnapshot(int currentCharId, CharTemplate[] AllEntities) : INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(currentCharId); writer.PutArray(AllEntities); }
    public void Deserialize(NetDataReader reader) { currentCharId = reader.GetInt(); AllEntities = reader.GetArray<CharTemplate>(); }
}
public record struct ExitSnapshot(int CharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
public record struct AttackSnapshot(int CharId): INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
public record struct MoveSnapshot(int CharId, GameCoord OldPosition, GameCoord NewPosition): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(OldPosition.X); writer.Put(OldPosition.Y); writer.Put(NewPosition.X); writer.Put(NewPosition.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); OldPosition = new GameCoord(reader.GetInt(), reader.GetInt()); NewPosition = new GameCoord(reader.GetInt(), reader.GetInt()); }
}
