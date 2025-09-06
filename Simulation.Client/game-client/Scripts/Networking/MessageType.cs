namespace GameClient.Scripts.Networking;

public enum MessageType : byte
{
    // Client -> Server
    EnterIntent,
    ExitIntent,
    AttackIntent,
    MoveIntent,
    TeleportIntent,

    // Server -> Client
    JoinAck,
    PlayerJoined,
    PlayerLeft,
    AttackSnapshot,
    MoveSnapshot,
    TeleportSnapshot,
    LoadMapSnapshot,
    UnloadMapSnapshot,
}
