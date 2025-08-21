using Arch.Core;
using Simulation.Core.Commons;

namespace Simulation.Core.Abstractions.In.Factories;

public interface IEntityFactory
{
    Entity CreateEntity(World world, int mapId, CharacterData data);
}