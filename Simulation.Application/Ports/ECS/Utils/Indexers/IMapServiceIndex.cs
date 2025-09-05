using Simulation.Application.Ports.Commons;
using Simulation.Application.Services;
using Simulation.Application.Services.ECS;

namespace Simulation.Application.Ports.ECS.Utils.Indexers;

/// <summary>
/// Define um serviço para mapear MapIds (int) para MapService.
/// </summary> 
public interface IMapServiceIndex : IIndex<int, MapService> { }