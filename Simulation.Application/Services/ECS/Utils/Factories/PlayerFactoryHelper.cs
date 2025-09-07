using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.ECS;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Services.ECS.Utils.Factories;

public class PlayerFactoryHelper : IFactoryHelper<PlayerState>
{
    // Base archetype: minimum components every player must have
    private static readonly ComponentType[] ArchetypeComponents = new[]
    {
        Component<CharId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType,
        Component<MoveStats>.ComponentType,
        Component<AttackStats>.ComponentType,
    };

    public ComponentType[] GetArchetype() => ArchetypeComponents;
    public void PopulateComponents(PlayerState data, Span<Action<World, Entity>> setters)
    {
        setters[0] = (world, e) => world.Set(e, new CharId { Value = data.CharId });
        setters[1] = (world, e) => world.Set(e, new MapId { Value = data.MapId });
        setters[2] = (world, e) => world.Set(e, data.Position);
        setters[3] = (world, e) => world.Set(e, data.Direction);
        setters[4] = (world, e) => world.Set(e, new MoveStats { Speed = data.MoveSpeed });
        setters[5] = (world, e) => world.Set(e, new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown });
        // Optional components via setters
        setters[6] = (world, e) => world.Set(e, new Blocking());
        // Ensure InCombat exists as a flag; semantics handled by combat systems
        setters[7] = (world, e) => world.Add<InCombat>(e);
    }

    public void ApplyTo(World world, Entity e, PlayerState data)
    {
        world.Set<
            CharId,
            MapId,
            Position,
            Direction,
            MoveStats,
            AttackStats
        >(e,
            new CharId { Value = data.CharId },
            new MapId { Value = data.MapId },
            data.Position,
            data.Direction,
            new MoveStats { Speed = data.MoveSpeed },
            new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown }
        );
    // Default optional components
    world.Add(e, new Blocking());
    world.Add<InCombat>(e);
    }
}