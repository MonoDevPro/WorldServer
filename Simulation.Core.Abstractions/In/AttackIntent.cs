using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.In;

/// <summary>
/// Comando unificado para iniciar qualquer tipo de ataque.
/// </summary>
public readonly record struct AttackIntent(int CharId);