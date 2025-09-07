using Godot;
using GameClient.Scripts.Network;
using Simulation.Domain.Components;

namespace GameClient.Scripts.Input;

/// <summary>
/// Handles player input and converts it to game intents
/// </summary>
public partial class InputHandler : Node
{
    private IntentService? _intentService;
    private bool _isConnected = false;
    
    public void Initialize(IntentService intentService)
    {
        _intentService = intentService;
    }
    
    public void SetConnected(bool connected)
    {
        _isConnected = connected;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!_isConnected || _intentService == null)
            return;
            
        // Handle movement input
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var input = new Simulation.Domain.Components.Input { X = 0, Y = 0 };
            bool shouldMove = false;
            
            switch (keyEvent.Keycode)
            {
                case Key.W:
                case Key.Up:
                    input.Y = -1;
                    shouldMove = true;
                    break;
                case Key.S:
                case Key.Down:
                    input.Y = 1;
                    shouldMove = true;
                    break;
                case Key.A:
                case Key.Left:
                    input.X = -1;
                    shouldMove = true;
                    break;
                case Key.D:
                case Key.Right:
                    input.X = 1;
                    shouldMove = true;
                    break;
                case Key.Space:
                    _intentService.SendAttackIntent();
                    break;
            }
            
            if (shouldMove)
            {
                _intentService.SendMoveIntent(input);
            }
        }
        
        // Handle mouse clicks for attack
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                _intentService.SendAttackIntent();
            }
        }
    }
    
    public override void _Process(double delta)
    {
        if (!_isConnected || _intentService == null)
            return;
            
        // Handle continuous movement input using actions
        var input = new Simulation.Domain.Components.Input { X = 0, Y = 0 };
        bool shouldMove = false;
        
        if (Godot.Input.IsActionPressed("ui_up") || Godot.Input.IsActionPressed("move_up"))
        {
            input.Y = -1;
            shouldMove = true;
        }
        else if (Godot.Input.IsActionPressed("ui_down") || Godot.Input.IsActionPressed("move_down"))
        {
            input.Y = 1;
            shouldMove = true;
        }
        
        if (Godot.Input.IsActionPressed("ui_left") || Godot.Input.IsActionPressed("move_left"))
        {
            input.X = -1;
            shouldMove = true;
        }
        else if (Godot.Input.IsActionPressed("ui_right") || Godot.Input.IsActionPressed("move_right"))
        {
            input.X = 1;
            shouldMove = true;
        }
        
        // Only send move intent occasionally to avoid spamming the server
        if (shouldMove)
        {
            var time = Time.GetUnixTimeFromSystem();
            var lastMoveTime = GetMeta("last_move_time", 0.0);
            
            if (time - (double)lastMoveTime > 0.1) // 100ms cooldown
            {
                _intentService.SendMoveIntent(input);
                SetMeta("last_move_time", time);
            }
        }
    }
}