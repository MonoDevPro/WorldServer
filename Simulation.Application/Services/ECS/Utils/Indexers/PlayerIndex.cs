using Simulation.Application.Ports.ECS.Utils.Indexers;

namespace Simulation.Application.Services.ECS.Utils.Indexers;

/// <summary>
/// Implementação em memória do ICharIndex. Herda de EntityIndex para reutilizar a lógica de dicionário.
/// Sua única responsabilidade é rastrear as entidades que estão atualmente na simulação.
/// </summary>
public class PlayerIndex : EntityIndex<int>, IPlayerIndex
{
}