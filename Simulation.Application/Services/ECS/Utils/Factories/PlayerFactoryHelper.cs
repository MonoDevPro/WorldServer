using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.ECS;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.ECS.Utils.Factories;

public class PlayerFactoryHelper : IFactoryHelper<PlayerStateDto>
{
    private static readonly ComponentType[] ArchetypeComponents = new[]
    {
        Component<CharId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType,
        Component<MoveStats>.ComponentType,
        Component<AttackStats>.ComponentType,
        Component<Blocking>.ComponentType,
    };

    public ComponentType[] GetArchetype() => ArchetypeComponents;
    public void PopulateComponents(PlayerStateDto data, Span<Action<World, Entity>> setters)
    {
        setters[0] = (world, e) => world.Set(e, new CharId { Value = data.CharId });
        setters[1] = (world, e) => world.Set(e, new MapId { Value = data.MapId });
        setters[2] = (world, e) => world.Set(e, data.Position);
        setters[3] = (world, e) => world.Set(e, data.Direction);
        setters[4] = (world, e) => world.Set(e, new MoveStats { Speed = data.MoveSpeed });
        setters[5] = (world, e) => world.Set(e, new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown });
        setters[6] = (world, e) => world.Set(e, new Blocking());
    }

    public void ApplyTo(World world, Entity e, PlayerStateDto data)
    {
        world.Set<
            CharId,
            MapId,
            Position,
            Direction,
            MoveStats,
            AttackStats,
            Blocking
        >(e,
            new CharId { Value = data.CharId },
            new MapId { Value = data.MapId },
            data.Position,
            data.Direction,
            new MoveStats { Speed = data.MoveSpeed },
            new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown },
            new Blocking()
            );
    }
}