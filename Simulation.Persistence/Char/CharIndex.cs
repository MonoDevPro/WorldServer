using Arch.Core;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Commons.Indexers;
using Simulation.Persistence.Commons;

namespace Simulation.Persistence.Char;

/// <summary>
/// Implementação em memória do ICharIndex. Herda de EntityIndex para reutilizar a lógica de dicionário.
/// Sua única responsabilidade é rastrear as entidades que estão atualmente na simulação.
/// </summary>
public class CharIndex : EntityIndex<int>, ICharIndex
{
}