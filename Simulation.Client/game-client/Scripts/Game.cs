using Arch.Core;
using GameClient.Scripts.ECS;
using GameClient.Scripts.Input;
using GameClient.Scripts.Networking;
using GameClient.Scripts.Config;
using Godot;

namespace GameClient.Scripts;

public partial class Game : Node
{
    private World _world = World.Create();
    private NetworkClient _network = new();
    private PlayerIndex _playerIndex = new();
    private SnapshotHandlerSystem? _snapshotSystem;
    private RenderSystem? _renderSystem;
    private InputHandler? _input;

    private Node _worldRoot = null!;
    private PackedScene _playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");

    public override void _Ready()
    {
        _worldRoot = GetNode<Node>("World");
        _snapshotSystem = new SnapshotHandlerSystem(_world, _playerIndex, _worldRoot, _playerScene);
    _renderSystem = new RenderSystem(_world, _worldRoot);
    _input = new InputHandler{ Network = _network };
    _input.SetContext(_world, _playerIndex);
    AddChild(_input);
    var cfg = ClientConfig.Load();
    _network.Connect(cfg.Host, cfg.Port, cfg.Key);
    _network.Connected += () => _network.Send(w => PacketProcessor.WriteEnterIntent(w, 1));
    }

    public override void _Process(double delta)
    {
        _network.PollEvents();
        var snapshots = _network.DequeueSnapshots();
        foreach (var s in snapshots)
        {
            if (s is DTOs.JoinAckDto)
            {
                // Recreate world cleanly to avoid leftover archetypes
                _world = World.Create();
                _playerIndex = new PlayerIndex();
                _snapshotSystem = new SnapshotHandlerSystem(_world, _playerIndex, _worldRoot, _playerScene);
                _renderSystem = new RenderSystem(_world, _worldRoot);
                _input?.SetContext(_world, _playerIndex);
                break; // only need once per batch
            }
        }
        _snapshotSystem?.Process(snapshots);
        if (_snapshotSystem != null && _input != null)
        {
            _input.LocalCharId = _snapshotSystem.LocalCharId == -1 ? _input.LocalCharId : _snapshotSystem.LocalCharId;
        }
        _renderSystem?.Process((float)delta);
    }
}
