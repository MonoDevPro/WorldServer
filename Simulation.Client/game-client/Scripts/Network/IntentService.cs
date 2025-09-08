using Godot;
using Simulation.Application.DTOs.Intents;
using Simulation.Domain;

namespace GameClient.Scripts.Network;

/// <summary>
/// Service responsible for sending intents to the server
/// </summary>
public class IntentService
{
    private readonly IClientPacketSender _packetSender;
    private int _localPlayerId;

    public IntentService(IClientPacketSender packetSender)
    {
        _packetSender = packetSender;
    }

    public void SetLocalPlayerId(int playerId)
    {
        _localPlayerId = playerId;
    }

    public void SendEnterIntent()
    {
        var intent = new EnterIntent(_localPlayerId);
        _packetSender.SendEnterIntent(intent);
        GD.Print($"Sent EnterIntent for player {_localPlayerId}");
    }

    public void SendMoveIntent(Simulation.Domain.Components.Input input)
    {
        var intent = new MoveIntent(_localPlayerId, input);
        _packetSender.SendMoveIntent(intent);
        GD.Print($"Sent MoveIntent for player {_localPlayerId}: ({input.X}, {input.Y})");
    }

    public void SendAttackIntent()
    {
        var intent = new AttackIntent(_localPlayerId);
        _packetSender.SendAttackIntent(intent);
        GD.Print($"Sent AttackIntent for player {_localPlayerId}");
    }

    public void SendExitIntent()
    {
        var intent = new ExitIntent(_localPlayerId);
        _packetSender.SendExitIntent(intent);
        GD.Print($"Sent ExitIntent for player {_localPlayerId}");
    }
}