using Simulation.Application.DTOs.Intents;

namespace GameClient.Scripts.Network;

/// <summary>
/// Simplified packet sender interface for the client
/// </summary>
public interface IClientPacketSender
{
    void SendEnterIntent(EnterIntent intent);
    void SendMoveIntent(MoveIntent intent);
    void SendAttackIntent(AttackIntent intent);
    void SendExitIntent(ExitIntent intent);
}

/// <summary>
/// Mock implementation for testing without a real server
/// </summary>
public class MockPacketSender : IClientPacketSender
{
    public void SendEnterIntent(EnterIntent intent)
    {
        Godot.GD.Print($"MOCK: Sending EnterIntent for CharId {intent.CharId}");
    }

    public void SendMoveIntent(MoveIntent intent)
    {
        Godot.GD.Print($"MOCK: Sending MoveIntent for CharId {intent.CharId}: ({intent.Input.X}, {intent.Input.Y})");
    }

    public void SendAttackIntent(AttackIntent intent)
    {
        Godot.GD.Print($"MOCK: Sending AttackIntent for CharId {intent.CharId}");
    }

    public void SendExitIntent(ExitIntent intent)
    {
        Godot.GD.Print($"MOCK: Sending ExitIntent for CharId {intent.CharId}");
    }
}