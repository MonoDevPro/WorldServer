using Simulation.Core.Abstractions.Ports.Char;
using Simulation.Core.Abstractions.Ports.Index;

namespace Simulation.Core.Abstractions.Adapters.Char;

/// <summary>
/// Implementação em memória do ICharIndex. Herda de EntityIndex para reutilizar a lógica de dicionário.
/// Sua única responsabilidade é rastrear as entidades que estão atualmente na simulação.
/// </summary>
public class CharIndex : EntityIndex<int>, ICharIndex
{
    // Herda toda a funcionalidade necessária de EntityIndex<int>.
    // Esta classe existe para que possamos registrá-la com uma interface específica (ICharIndex)
    // na injeção de dependência, mantendo o código desacoplado.
}
