using Arch.Core;
using Simulation.Application.Ports.Commons.Factories;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Map;

public interface IMapFactory : IFactory<Entity, MapTemplate>, IArchetypeProvider<MapTemplate>, IQueryProvider<MapTemplate>
{
    
}