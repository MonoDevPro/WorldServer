using Arch.Core;
using Arch.Core.Extensions;
using GameClient.Scripts.Domain;
using GameClient.Scripts.DTOs;
using GameClient.Scripts.Rendering;
using Godot;

namespace GameClient.Scripts.ECS;

public class SnapshotHandlerSystem
{
    private readonly World _world;
    private readonly PlayerIndex _index;
    private readonly Node _worldRoot;
    private readonly PackedScene _playerScene;
    private int _localCharId = -1;
    public int LocalCharId => _localCharId;

    public SnapshotHandlerSystem(World world, PlayerIndex index, Node worldRoot, PackedScene playerScene)
    {
        _world = world; _index = index; _worldRoot = worldRoot; _playerScene = playerScene;
    }

    public void Process(IEnumerable<object> snapshots)
    {
        foreach (var s in snapshots)
        {
            switch (s)
            {
                case JoinAckDto join:
                    HandleJoin(join);
                    break;
                case PlayerJoinedDto pj:
                    SpawnOrUpdate(pj.NewPlayer, isLocal:false);
                    break;
                case PlayerLeftDto pl:
                    if (_index.TryGet(pl.LeftPlayer.CharId, out var ent))
                    {
                        _world.Destroy(ent);
                        _index.Remove(pl.LeftPlayer.CharId);
                        // Remove visual
                        foreach (Node child in _worldRoot.GetChildren())
                        {
                            if (child is PlayerView pv && pv.CharId.Value == pl.LeftPlayer.CharId)
                            { child.QueueFree(); break; }
                        }
                    }
                    break;
                case MoveSnapshot mv:
                    if (_index.TryGet(mv.CharId, out var e))
                    {
                        ref var pos = ref e.Get<Position>();
                        var old = pos;
                        pos = mv.New; // authoritative
                        if (e.Has<Interpolation>())
                        {
                            ref var interp = ref e.Get<Interpolation>();
                            interp.StartX = old.X; interp.StartY = old.Y;
                            interp.TargetX = mv.New.X; interp.TargetY = mv.New.Y;
                            interp.Elapsed = 0f; interp.Duration = 0.1f; // 100ms smoothing
                        }
                        else
                        {
                            e.Add(new Interpolation
                            {
                                StartX = old.X, StartY = old.Y,
                                TargetX = mv.New.X, TargetY = mv.New.Y,
                                CurrentX = mv.New.X, CurrentY = mv.New.Y,
                                Duration = 0.1f, Elapsed = 0f
                            });
                        }
                    }
                    break;
                case TeleportSnapshot tp:
                    if (_index.TryGet(tp.CharId, out var e2))
                    {
                        ref var pos = ref e2.Get<Position>();
                        pos = tp.Position;
                        if (e2.Has<Interpolation>())
                        {
                            ref var interp = ref e2.Get<Interpolation>();
                            interp.StartX = tp.Position.X;
                            interp.StartY = tp.Position.Y;
                            interp.TargetX = tp.Position.X;
                            interp.TargetY = tp.Position.Y;
                            interp.CurrentX = tp.Position.X;
                            interp.CurrentY = tp.Position.Y;
                            interp.Elapsed = 0f; interp.Duration = 0f;
                        }
                        // Map change ignored for now
                    }
                    break;
                case AttackSnapshot atk:
                    // TODO: play animation / effect
                    break;
            }
        }
    }

    private void HandleJoin(JoinAckDto join)
    {
        // Clear existing world
        _index.Clear();
        // NOTE: Arch.Core doesn't yet have a built-in Clear? We'll just create fresh world approach in future.
        _localCharId = join.YourCharId;
        // Others
        foreach (var other in join.Others)
            SpawnOrUpdate(other, isLocal: other.CharId == join.YourCharId);
    }

    private void SpawnOrUpdate(PlayerStateDto state, bool isLocal)
    {
        if (_index.TryGet(state.CharId, out var existing))
        {
            ref var pos = ref existing.Get<Position>(); pos = state.Position;
            ref var dir = ref existing.Get<Direction>(); dir = state.Direction;
            return;
        }
        // Build components list dynamically so we only add LocalPlayer tag when needed
        var components = new List<object>(9)
        {
            new CharId{ Value = state.CharId },
            new MapId{ Value = state.MapId },
            state.Position,
            state.Direction,
            new MoveStats{ Speed = state.MoveSpeed },
            new AttackStats{ CastTime = state.AttackCastTime, Cooldown = state.AttackCooldown },
            new Interpolation
            {
                StartX = state.Position.X,
                StartY = state.Position.Y,
                TargetX = state.Position.X,
                TargetY = state.Position.Y,
                CurrentX = state.Position.X,
                CurrentY = state.Position.Y,
                Duration = 0f,
                Elapsed = 0f
            }
        };
        if (isLocal) components.Add(new LocalPlayer());
        var entity = _world.Create(components.ToArray());
        _index.Add(state.CharId, entity);
        // Visual
        var node = _playerScene.Instantiate<PlayerView>();
        node.Entity = entity;
        node.CharId = new CharId{ Value = state.CharId };
        // simple color distinction
        var colorRect = new ColorRect{ Color = isLocal ? Colors.LimeGreen : Colors.CornflowerBlue, Size = new Vector2(8,8) };
        node.AddChild(colorRect);
        _worldRoot.AddChild(node);
    }
}
