using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Domain.Templates;
using Simulation.Persistence.Commons;

namespace Simulation.Persistence.Char;

/// <summary>
/// Implementação em memória do ICharTemplateRepository.
/// Simula um banco de dados carregando templates pré-definidos.
/// </summary>
public class CharTemplateIndex : DefaultIndex<int, CharTemplate>, ICharTemplateIndex
{
}