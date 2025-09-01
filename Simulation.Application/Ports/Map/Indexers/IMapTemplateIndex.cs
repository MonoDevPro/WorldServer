using Simulation.Application.Ports.Commons.Indexers;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Map.Indexers;

/// <summary>
/// Define um serviço para mapear CharTemplateIds (int) para CharTemplates.
/// </summary>
public interface IMapTemplateIndex : IIndex<int, MapTemplate>{}