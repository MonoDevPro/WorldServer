using LiteNetLib.Utils;

namespace Simulation.Core.Abstractions.Intents.In;

/// <summary>
/// Comando unificado para iniciar qualquer tipo de ataque.
/// </summary>
public record struct AttackIntent(int AttackerCharId) : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(AttackerCharId);
    }

    public void Deserialize(NetDataReader reader)
    {
        AttackerCharId = reader.GetInt();
    }
}