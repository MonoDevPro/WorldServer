using Arch.Core;
using Simulation.Application.Options;
using Simulation.Application.Ports.Commons.Factories;

namespace Simulation.Factories;

public static class WorldFactory
{
    public static World Create(WorldOptions data)
    {
        return World.Create(
            data.EntityCapacity, 
            data.ArchetypeCapacity, 
            data.ChunkSizeInBytes, 
            data.MinimumAmountOfEntitiesPerChunk);
    }
}