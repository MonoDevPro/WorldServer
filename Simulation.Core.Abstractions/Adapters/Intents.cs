using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters;

public record struct EnterIntent(int CharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
public record struct ExitIntent(int CharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
public record struct AttackIntent(int AttackerCharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer) => writer.Put(AttackerCharId);
    public void Deserialize(NetDataReader reader) => AttackerCharId = reader.GetInt();
}
public record struct MoveIntent(int CharId, Input Input): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(Input.X); writer.Put(Input.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); Input = new Input{ X = reader.GetInt(), Y = reader.GetInt()}; }
}
public record struct TeleportIntent(int CharId, int TargetMapId, Position TargetPos): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(TargetMapId);  writer.Put(TargetPos.X); writer.Put(TargetPos.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); TargetMapId = reader.GetInt(); TargetPos = new Position{ X = reader.GetInt(), Y = reader.GetInt() }; }
}
public record struct TeleportSnapshot(int CharId, int MapId, Position Position): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(MapId);  writer.Put(Position.X); writer.Put(Position.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); MapId = reader.GetInt(); Position = new Position{ X = reader.GetInt(), Y = reader.GetInt() }; }
}