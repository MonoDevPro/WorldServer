using Arch.Core;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Map;

public static class WorldFactory
{
    public static Entity CreateEntityFromRuntimeTemplate(World world, CharRuntimeTemplate tpl)
    {
        return world.Create(
            tpl.CharId,
            tpl.MapId,
            tpl.Position,
            tpl.Direction,
            tpl.MoveStats,
            tpl.AttackStats,
            tpl.Blocking
        );
    }
    
    public static CharRuntimeTemplate CreateRuntimeTemplate(CharTemplate tpl)
    {
        return new CharRuntimeTemplate
        {
            CharId = new CharId { Value = tpl.CharId },
            MapId = new MapId { Value = tpl.MapId },
            Position = new Position { Value = tpl.Position },
            Direction = new Direction { Value = tpl.Direction },
            MoveStats = new MoveStats { Speed = tpl.MoveSpeed },
            AttackStats = new AttackStats
            {
                CastTime = tpl.AttackCastTime,
                Cooldown = tpl.AttackCooldown
            },
            Blocking = new Blocking()
        };
    }
    
    public static CharRuntimeTemplate CreateRuntimeTemplate(World world, Entity entity)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (!world.IsAlive(entity)) throw new ArgumentException("Entity is not alive in the given world.", nameof(entity));

        ref var cid = ref world.Get<CharId>(entity);
        ref var mid = ref world.Get<MapId>(entity);
        ref var pos = ref world.Get<Position>(entity);
        ref var dir = ref world.Get<Direction>(entity);
        ref var mv = ref world.Get<MoveStats>(entity);
        ref var atk = ref world.Get<AttackStats>(entity);

        return new CharRuntimeTemplate
        {
            CharId = cid,
            MapId = mid,
            Position = pos,
            Direction = dir,
            MoveStats = mv,
            AttackStats = atk,
            Blocking = new Blocking()
        };
    }

    public static CharTemplate UpdateCharTemplate(CharTemplate template, World world, Entity entity)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (!world.IsAlive(entity)) throw new ArgumentException("Entity is not alive in the given world.", nameof(entity));

        ref var cid = ref world.Get<CharId>(entity);
        ref var mid = ref world.Get<MapId>(entity);
        ref var pos = ref world.Get<Position>(entity);
        ref var dir = ref world.Get<Direction>(entity);
        ref var mv = ref world.Get<MoveStats>(entity);
        ref var atk = ref world.Get<AttackStats>(entity);

        template.CharId = cid.Value;
        template.MapId = mid.Value;
        template.Position = pos.Value;
        template.Direction = dir.Value;
        template.MoveSpeed = mv.Speed;
        template.AttackCastTime = atk.CastTime;
        template.AttackCooldown = atk.Cooldown;

        return template;
    }
}