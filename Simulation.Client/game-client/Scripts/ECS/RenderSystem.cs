using Arch.Core;
using GameClient.Scripts.Domain;
using Arch.Core.Extensions;
using GameClient.Scripts.Rendering;
using Godot;

namespace GameClient.Scripts.ECS;

public class RenderSystem
{
    private readonly World _world;
    private readonly Node _worldRoot;
    private readonly QueryDescription _query = new QueryDescription().WithAll<CharId, Position>();

    public RenderSystem(World world, Node worldRoot)
    { _world = world; _worldRoot = worldRoot; }

    public void Process(float dt)
    {
        // Update interpolation components
    _world.Query(in _query, (ref Entity e, ref CharId id, ref Position pos) =>
        {
            if (e.Has<Interpolation>())
            {
                ref var interp = ref e.Get<Interpolation>();
                if (interp.Duration > 0f)
                {
                    interp.Elapsed += dt;
                    var t = Mathf.Clamp(interp.Elapsed / interp.Duration, 0f, 1f);
                    t = t * t * (3f - 2f * t);
                    interp.CurrentX = interp.StartX + (interp.TargetX - interp.StartX) * t;
                    interp.CurrentY = interp.StartY + (interp.TargetY - interp.StartY) * t;
                }
                else
                {
                    interp.CurrentX = interp.TargetX;
                    interp.CurrentY = interp.TargetY;
                }
            }
        });

        // Apply to visuals
        foreach (Node child in _worldRoot.GetChildren())
        {
            if (child is not PlayerView pv) continue;
            var targetId = pv.CharId.Value;
            _world.Query(in _query, (ref Entity e, ref CharId id, ref Position pos) =>
            {
                if (id.Value == targetId)
                {
                    if (e.Has<Interpolation>())
                        pv.Position = new Vector2(e.Get<Interpolation>().CurrentX, e.Get<Interpolation>().CurrentY);
                    else
                        pv.Position = new Vector2(pos.X, pos.Y);
                }
            });
        }
    }
}
