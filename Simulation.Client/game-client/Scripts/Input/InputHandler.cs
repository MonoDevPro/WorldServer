using GameClient.Scripts.Networking;
using GameClient.Scripts.ECS;
using GameClient.Scripts.Domain;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;

namespace GameClient.Scripts.Input;

public partial class InputHandler : Node
{
    public int LocalCharId { get; set; } = 1; // placeholder
    public NetworkClient? Network;
    private World? _world;
    private PlayerIndex? _index;
    public void SetContext(World world, PlayerIndex index)
    {
        _world = world; _index = index;
    }
    private float _sendAccumulator;
    private const float SendInterval = 0.1f; // send move intent 10Hz while keys held

    public override void _Process(double delta)
    {
        if (Network == null || !Network.IsConnected) return;
        _sendAccumulator += (float)delta;
        if (_sendAccumulator < SendInterval) return;
        _sendAccumulator = 0f;

        int x=0,y=0;
        if (Godot.Input.IsActionPressed("ui_right")) x += 1;
        if (Godot.Input.IsActionPressed("ui_left")) x -= 1;
        if (Godot.Input.IsActionPressed("ui_down")) y += 1;
        if (Godot.Input.IsActionPressed("ui_up")) y -= 1;
        if (x!=0 || y!=0)
        {
            Network.Send(w => PacketProcessor.WriteMoveIntent(w, LocalCharId, x, y));
            PredictMove(x,y);
        }
        if (Godot.Input.IsActionJustPressed("ui_accept"))
        {
            Network.Send(w => PacketProcessor.WriteAttackIntent(w, LocalCharId));
        }
    }

    private void PredictMove(int dx, int dy)
    {
        if (_world == null || _index == null) return;
        if (!_index.TryGet(LocalCharId, out var entity)) return;
        ref var pos = ref entity.Get<Position>();
        var newPos = new Position{ X = pos.X + dx, Y = pos.Y + dy };
        var oldPos = pos;
        pos = newPos; // immediate client-side position
        if (entity.Has<Interpolation>())
        {
            ref var interp = ref entity.Get<Interpolation>();
            interp.StartX = oldPos.X; interp.StartY = oldPos.Y;
            interp.TargetX = newPos.X; interp.TargetY = newPos.Y;
            interp.Elapsed = 0f; interp.Duration = 0.08f; // a bit faster locally
        }
        else
        {
            entity.Add(new Interpolation
            {
                StartX = oldPos.X, StartY = oldPos.Y,
                TargetX = newPos.X, TargetY = newPos.Y,
                CurrentX = newPos.X, CurrentY = newPos.Y,
                Duration = 0.08f, Elapsed = 0f
            });
        }
    }
}
