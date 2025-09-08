using Simulation.Application.DTOs;
using Simulation.Domain;

namespace GameClient.Scripts.Network.Packets;

// Simplified packet classes for the client
// These mirror the server packets but don't require the full networking infrastructure

public class ClientJoinAckSnapshot
{
    public int YourCharId { get; set; }
    public int MapId { get; set; }
    public List<PlayerState> Others { get; set; } = new();
}

public class ClientPlayerJoinedSnapshot
{
    public PlayerState NewPlayer { get; set; } = new();
}

public class ClientPlayerLeftSnapshot
{
    public PlayerState LeftPlayer { get; set; } = new();
}

public class ClientMoveSnapshot
{
    public int CharId { get; set; }
    public Position Old { get; set; }
    public Position New { get; set; }
}

public class ClientAttackSnapshot
{
    public int CharId { get; set; }
}

public class ClientTeleportSnapshot
{
    public int CharId { get; set; }
    public int MapId { get; set; }
    public Position Position { get; set; }
}