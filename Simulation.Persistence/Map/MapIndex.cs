using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Application.Services;
using Simulation.Persistence.Commons;

namespace Simulation.Persistence.Map;

public sealed class MapIndex : DefaultIndex<int, MapService>, IMapIndex
{
}