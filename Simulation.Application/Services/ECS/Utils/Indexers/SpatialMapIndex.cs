using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Application.Services.Commons;

namespace Simulation.Application.Services.ECS.Utils.Indexers;

public sealed class SpatialMapIndex : DefaultIndex<int, MapService>, IMapServiceIndex
{
}