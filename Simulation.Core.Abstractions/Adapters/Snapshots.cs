using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters;

public record struct EnterSnapshot(int mapId, int charId, CharTemplate[] templates) : INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(mapId); writer.Put(charId); writer.PutArray(templates); }
    public void Deserialize(NetDataReader reader) { mapId = reader.GetInt(); charId = reader.GetInt(); templates = reader.GetArray<CharTemplate>(() => new CharTemplate()); }
}
public record struct CharSnapshot(int MapId, int CharId, CharTemplate Template) : INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(MapId); writer.Put(CharId); writer.Put(Template); }

    public void Deserialize(NetDataReader reader) { MapId = reader.GetInt(); CharId = reader.GetInt(); Template = new CharTemplate(); reader.Get(() => new CharTemplate()); }
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
public record struct MoveSnapshot(int CharId, Position OldPosition, Position NewPosition): INetSerializable
{
    public void Serialize(NetDataWriter writer) { writer.Put(CharId); writer.Put(OldPosition.X); writer.Put(OldPosition.Y); writer.Put(NewPosition.X); writer.Put(NewPosition.Y); }
    public void Deserialize(NetDataReader reader) { CharId = reader.GetInt(); OldPosition = new Position{ X = reader.GetInt(), Y = reader.GetInt() }; NewPosition = new Position{ X = reader.GetInt(), Y = reader.GetInt() }; }
}