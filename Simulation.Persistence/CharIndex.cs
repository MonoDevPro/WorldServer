using Simulation.Application.Ports.Char;

namespace Simulation.Persistence;

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
