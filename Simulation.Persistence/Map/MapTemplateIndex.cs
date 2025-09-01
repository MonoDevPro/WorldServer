using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Domain.Templates;
using Simulation.Persistence.Commons;

namespace Simulation.Persistence.Map;

/// <summary>
/// Implementação em memória do ICharTemplateRepository.
/// Simula um banco de dados carregando templates pré-definidos.
/// </summary>
public class MapTemplateIndex : DefaultIndex<int, MapTemplate>, IMapTemplateIndex
{
}