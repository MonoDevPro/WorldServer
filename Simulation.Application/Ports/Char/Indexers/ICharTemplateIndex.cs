using Simulation.Application.Ports.Commons.Indexers;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Char.Indexers;

/// <summary>
/// Define um serviço para mapear CharTemplateIds (int) para CharTemplates.
/// </summary>
public interface ICharTemplateIndex : IIndex<int, CharTemplate>, IReverseIndex<int, CharTemplate> { }