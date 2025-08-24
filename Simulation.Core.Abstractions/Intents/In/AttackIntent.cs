namespace Simulation.Core.Abstractions.Intents.In;

/// <summary>
/// Comando unificado para iniciar qualquer tipo de ataque.
/// </summary>
public readonly record struct AttackIntent(int AttackerCharId);