using LiteNetLib.Utils;

namespace Simulation.ECS.Events;

public class WorldStateSnapshot : INetSerializable
{
    // A lista de todos os jogadores visíveis e seus estados.
    public List<PlayerStateSnapshot> PlayerStates { get; set; } = new();
    
    // Você poderia adicionar outros estados aqui (NPCs, projéteis, etc.)
    // public List<NpcStateSnapshot> NpcStates { get; set; } = new();

    public void Serialize(NetDataWriter writer)
    {
        // Escreve a contagem de jogadores e depois cada jogador.
        writer.Put(PlayerStates.Count);
        foreach (var playerState in PlayerStates)
            playerState.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        int playerCount = reader.GetInt();
        PlayerStates = new List<PlayerStateSnapshot>(playerCount);
        for (int i = 0; i < playerCount; i++)
        {
            var snapshot = new PlayerStateSnapshot();
            snapshot.Deserialize(reader);
            PlayerStates.Add(snapshot);
        }
    }
}