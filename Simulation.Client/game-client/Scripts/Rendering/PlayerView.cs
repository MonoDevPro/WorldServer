using Arch.Core;
using Godot;
using Simulation.Domain;

namespace GameClient.Scripts.Rendering;

/// <summary>
/// Handles rendering for an individual player entity
/// </summary>
public partial class PlayerView : Node2D
{
    private Entity? _entity;
    private World? _world;
    private Vector2 _targetPosition;
    private Vector2 _currentPosition;
    private int _charId;
    
    // Visual components
    private ColorRect _playerRect = null!;
    private Label _playerLabel = null!;
    
    public override void _Ready()
    {
        // Create visual representation
        _playerRect = new ColorRect
        {
            Size = new Vector2(32, 32),
            Position = new Vector2(-16, -16), // Center the rectangle
            Color = Colors.Blue
        };
        AddChild(_playerRect);
        
        _playerLabel = new Label
        {
            Position = new Vector2(-16, -40),
            Text = "Player"
        };
        AddChild(_playerLabel);
    }
    
    public void Initialize(Entity entity, World world, int charId)
    {
        _entity = entity;
        _world = world;
        _charId = charId;
        
        // Set initial position from entity
        if (_world.TryGet<Position>(_entity.Value, out var position))
        {
            _targetPosition = new Vector2(position.X * 32, position.Y * 32); // Scale to pixels
            _currentPosition = _targetPosition;
            Position = _currentPosition;
        }
        
        // Update label
        _playerLabel.Text = $"Player {_charId}";
        
        // Set different color for local player (assuming player 1 is local for now)
        if (_charId == 1) // This should be set properly based on local player ID
        {
            _playerRect.Color = Colors.Green;
        }
    }
    
    public override void _Process(double delta)
    {
        if (_entity == null || _world == null)
            return;
            
        // Read position from ECS entity
        if (_world.TryGet<Position>(_entity.Value, out var position))
        {
            _targetPosition = new Vector2(position.X * 32, position.Y * 32); // Scale to pixels
        }
        
        // Smooth interpolation to target position
        _currentPosition = _currentPosition.Lerp(_targetPosition, (float)(delta * 10.0)); // Adjust speed as needed
        Position = _currentPosition;
    }
    
    public void SetAsLocalPlayer()
    {
        _playerRect.Color = Colors.Green;
    }
    
    public void SetAsRemotePlayer()
    {
        _playerRect.Color = Colors.Blue;
    }
}