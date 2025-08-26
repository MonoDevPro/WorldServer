using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.In;

public record struct TeleportIntent(int CharId, GameVector2 Target): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(Target.X);
        writer.Put(Target.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        Target = new GameVector2(reader.GetInt(), reader.GetInt());
    }
}