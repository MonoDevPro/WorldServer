using Arch.Core;
using GameClient.Scripts.Domain;
using Godot;

namespace GameClient.Scripts.Rendering;

public partial class PlayerView : Node2D
{
    public Entity Entity { get; set; }
    public CharId CharId; // optional cached
}
