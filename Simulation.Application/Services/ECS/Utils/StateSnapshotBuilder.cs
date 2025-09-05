using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.ECS.Utils;
using Simulation.Domain.Components;

namespace Simulation.Application.Services.ECS.Utils;

public class StateSnapshotBuilder : IStateSnapshotBuilder
{
    public PlayerStateDto BuildCharState(World world, Entity e)
    {
        // Use TryGet/Get conforme a certeza de existência de componentes.
        ref var cid = ref world.Get<CharId>(e).Value;
        ref var mid = ref world.Get<MapId>(e).Value;
        ref var pos = ref world.Get<Position>(e);
        ref var dir = ref world.Get<Direction>(e);
        ref var move = ref world.Get<MoveStats>(e);
        ref var atk = ref world.Get<AttackStats>(e);

        return new PlayerStateDto(
            CharId: cid,
            EntityId: e.Id,
            MapId: mid,
            Position: pos,
            Direction: dir,
            MoveSpeed: move.Speed,
            AttackCastTime: atk.CastTime,
            AttackCooldown: atk.Cooldown
        );
    }
}