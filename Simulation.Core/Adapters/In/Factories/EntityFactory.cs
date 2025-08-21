using Arch.Core;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Core.Adapters.In.Factories;

public class EntityFactory : IEntityFactory
{
    private readonly IEntityIndex _index;

    public EntityFactory(IEntityIndex index)
    {
        _index = index;
    }

    public Entity CreateEntity(World world, int mapId, CharacterData data)
    {
        var entity = world.Create(
            new CharId { CharacterId = data.Id },
            new CharInfo { Name = data.Name, Gender = data.Gender, Vocation = data.Vocation },
            new MapRef{ MapId = mapId },
            new TilePosition { Position = data.Position },
            new TileVelocity { Velocity = new VelocityVector(0,0) },
            new AttackStats { Duration = 1f, Cooldown = 1f },
            new MoveSpeed { Value = data.Speed },
            new Blocking { }
        );
        _index.Register(data.Id, in entity);
        return entity;
    }
}