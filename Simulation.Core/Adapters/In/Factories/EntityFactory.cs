using Arch.Core;
using Simulation.Core.Abstractions.In.Factories;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Core.Adapters.In.Factories;

public class EntityFactory : IEntityFactory
{
    public Entity CreateEntity(World world, int mapId, CharacterData data)
    {
        return world.Create(
            new CharId { CharacterId = data.Id },
            new CharInfo { Name = data.Name, Gender = data.Gender, Vocation = data.Vocation },
            new MapRef{ MapId = mapId },
            new TilePosition { Position = data.Position },
            new TileVelocity { Velocity = new VelocityVector(0,0) },
            new AttackStats { Duration = 1f, Cooldown = 1f },
            new MoveSpeed { Value = data.Speed },
            new Blocking { }
        );
    }
}