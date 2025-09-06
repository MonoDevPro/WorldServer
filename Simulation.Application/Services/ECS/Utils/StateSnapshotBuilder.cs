using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.ECS.Utils;
using Simulation.Domain.Components;

namespace Simulation.Application.Services.ECS.Utils;

public class StateSnapshotBuilder : IStateSnapshotBuilder
{
    public PlayerStateDto BuildCharState(World world, Entity e)
    {
        // Acessos defensivos: em ambientes de desenvolvimento podemos ter entidades legacy
        // criadas antes de adicionar novos componentes. Evitar AccessViolation/segfault do Arch.
        int charId = 0;
        int mapId = 0;
        Position position = default;
        Direction direction = default;
        MoveStats moveStats = default;
        AttackStats attackStats = default;

        if (world.Has<CharId>(e)) charId = world.Get<CharId>(e).Value;
        if (world.Has<MapId>(e)) mapId = world.Get<MapId>(e).Value;
        if (world.Has<Position>(e)) position = world.Get<Position>(e);
        if (world.Has<Direction>(e)) direction = world.Get<Direction>(e);
        if (world.Has<MoveStats>(e)) moveStats = world.Get<MoveStats>(e);
        if (world.Has<AttackStats>(e)) attackStats = world.Get<AttackStats>(e);

        return new PlayerStateDto(
            CharId: charId,
            EntityId: e.Id,
            MapId: mapId,
            Position: position,
            Direction: direction,
            MoveSpeed: moveStats.Speed,
            AttackCastTime: attackStats.CastTime,
            AttackCooldown: attackStats.Cooldown
        );
    }
}