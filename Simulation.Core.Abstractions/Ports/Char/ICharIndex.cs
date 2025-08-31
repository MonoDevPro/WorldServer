using Simulation.Core.Abstractions.Ports.Index;

namespace Simulation.Core.Abstractions.Ports.Char;

/// <summary>
/// Define um serviço para mapear CharIds (int) para Entidades vivas na simulação.
/// </summary>
public interface ICharIndex : IEntityIndex<int>
{
    // No futuro, você pode adicionar métodos específicos aqui, como:
    // bool TryGetPosition(int charId, out Position pos);
}
