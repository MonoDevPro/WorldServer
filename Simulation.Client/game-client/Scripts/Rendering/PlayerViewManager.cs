using Arch.Core;
using Godot;
using GameClient.Scripts.State;

namespace GameClient.Scripts.Rendering;

/// <summary>
/// Manages the creation and destruction of PlayerView instances based on ECS entities
/// </summary>
public partial class PlayerViewManager : Node2D
{
    private readonly Dictionary<int, PlayerView> _playerViews = new();
    private SnapshotApplySystem? _snapshotSystem;
    private World? _world;
    private PackedScene _playerScene = null!;
    private int _localPlayerId;
    
    public override void _Ready()
    {
        _playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");
    }
    
    public void Initialize(SnapshotApplySystem snapshotSystem, World world)
    {
        _snapshotSystem = snapshotSystem;
        _world = world;
    }
    
    public void SetLocalPlayerId(int playerId)
    {
        _localPlayerId = playerId;
        
        // Update existing player view if it exists
        if (_playerViews.TryGetValue(playerId, out var playerView))
        {
            playerView.SetAsLocalPlayer();
        }
    }
    
    public override void _Process(double delta)
    {
        if (_snapshotSystem == null || _world == null)
            return;
            
        // Check for new players that need views
        foreach (var (charId, entity) in _snapshotSystem.GetAllPlayerEntities())
        {
            if (!_playerViews.ContainsKey(charId))
            {
                CreatePlayerView(charId, entity);
            }
        }
        
        // Check for players that no longer exist
        var idsToRemove = new List<int>();
        foreach (var charId in _playerViews.Keys)
        {
            var entity = _snapshotSystem.GetPlayerEntity(charId);
            if (entity == null)
            {
                idsToRemove.Add(charId);
            }
        }
        
        foreach (var charId in idsToRemove)
        {
            RemovePlayerView(charId);
        }
    }
    
    private void CreatePlayerView(int charId, Entity entity)
    {
        var playerViewNode = _playerScene.Instantiate<PlayerView>();
        AddChild(playerViewNode);
        
        playerViewNode.Initialize(entity, _world!, charId);
        
        // Set color based on whether this is the local player
        if (charId == _localPlayerId)
        {
            playerViewNode.SetAsLocalPlayer();
        }
        else
        {
            playerViewNode.SetAsRemotePlayer();
        }
        
        _playerViews[charId] = playerViewNode;
        GD.Print($"Created PlayerView for CharId {charId}");
    }
    
    private void RemovePlayerView(int charId)
    {
        if (_playerViews.TryGetValue(charId, out var playerView))
        {
            playerView.QueueFree();
            _playerViews.Remove(charId);
            GD.Print($"Removed PlayerView for CharId {charId}");
        }
    }
}