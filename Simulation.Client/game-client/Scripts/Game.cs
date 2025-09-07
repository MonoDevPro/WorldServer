using Arch.Core;
using Godot;
using Simulation.Application.DTOs.Snapshots;

namespace GameClient.Scripts;

public partial class Game : Node
{
    private Node _worldRoot = null!;
    private PackedScene _playerScene = GD.Load<PackedScene>("res://Scenes/Player.tscn");

    public override void _Ready()
    {
        _worldRoot = GetNode<Node>("World");
    }

    public override void _Process(double delta)
    {
    }
}
