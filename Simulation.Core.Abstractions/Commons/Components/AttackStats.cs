namespace Simulation.Core.Components;

/// <summary>
/// Define os atributos de combate de uma entidade.
/// </summary>
public struct AttackStats
{
    /// <summary>
    /// O tempo em segundos que a entidade leva para preparar o ataque (cast time).
    /// </summary>
    public float Duration;

    /// <summary>
    /// O tempo total de recarga em segundos entre o início de um ataque e o início do próximo.
    /// </summary>
    public float Cooldown;
}