using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters;

public record struct EnterGameIntent(int CharacterId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharacterId);
    public void Deserialize(NetDataReader reader) => CharacterId = reader.GetInt();
}
public record struct ExitGameIntent(int CharacterId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharacterId);
    public void Deserialize(NetDataReader reader) => CharacterId = reader.GetInt();
}
public record struct AttackIntent(int AttackerCharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(AttackerCharId);
    public void Deserialize(NetDataReader reader) => AttackerCharId = reader.GetInt();
}
public record struct MoveIntent(int CharId, GameCoord Input): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(Input.X); writer.Put(Input.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); Input = new GameCoord(reader.GetInt(), reader.GetInt()); }
}
public record struct TeleportIntent(int CharId, GameCoord Target): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(Target.X); writer.Put(Target.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); Target = new GameCoord(reader.GetInt(), reader.GetInt()); }
}
public record struct TeleportSnapshot(int CharId, GameCoord Position): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(Position.X); writer.Put(Position.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); Position = new GameCoord(reader.GetInt(), reader.GetInt()); }
}