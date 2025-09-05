using Simulation.Application.Ports.Commons.Indexers;
using Simulation.Application.Services;

namespace Simulation.Application.Ports.Map.Indexers;

/// <summary>
/// Define um serviço para mapear MapIds (int) para MapService.
/// </summary> 
public interface IMapServiceIndex : IIndex<int, MapService> { }