using LiteNetLib.Utils;

namespace Simulation.ECS.Events;

/// <summary>
/// Representa uma "foto" do estado de um jogador em um determinado momento,
/// otimizada para ser serializada e enviada pela rede.
/// </summary>
public struct PlayerStateSnapshot : INetSerializable
{
    // --- Dados do Snapshot ---
    public int CharId { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int DirX { get; set; }
    public int DirY { get; set; }
    public int CurrentHealth { get; set; }
    
    // Você poderia adicionar mais estados aqui, como flags (correndo, atacando, etc.)
    // public byte StateFlags { get; set; }

    /// <summary>
    /// Método para escrever os dados desta struct em um buffer de rede.
    /// A ordem de escrita é crucial e deve ser a mesma da leitura.
    /// </summary>
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(PosX);
        writer.Put(PosY);
        writer.Put(DirX);
        writer.Put(DirY);
        writer.Put(CurrentHealth);
    }

    /// <summary>
    /// Método para ler os dados de um buffer de rede e preencher esta struct.
    /// A ordem de leitura deve ser idêntica à de escrita.
    /// </summary>
    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        PosX = reader.GetInt();
        PosY = reader.GetInt();
        DirX = reader.GetInt();
        DirY = reader.GetInt();
        CurrentHealth = reader.GetInt();
    }
}
