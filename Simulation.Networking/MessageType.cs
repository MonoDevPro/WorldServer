namespace Simulation.Networking;

public enum MessageType : byte
{
    // Client to Server (Intents)
    EnterIntent,
    ExitIntent,
    AttackIntent,
    MoveIntent,
    TeleportIntent,

    // Server to Client (Snapshots / DTOs)
    JoinAck,
    PlayerJoined,
    PlayerLeft,
    AttackSnapshot,
    MoveSnapshot,
    TeleportSnapshot,
    LoadMapSnapshot, // Embora não haja DTO, incluímos para completude
    UnloadMapSnapshot, // Embora não haja DTO, incluímos para completude
}