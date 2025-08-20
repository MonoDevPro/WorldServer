namespace Simulation.Core.Components;

public enum AttackPhase
{
    /// <summary>
    /// Pronto para iniciar um novo ataque.
    /// </summary>
    Ready,
    /// <summary>
    /// Preparando o ataque (durante o cast time).
    /// </summary>
    Casting,
    /// <summary>
    /// Em tempo de recarga após um ataque.
    /// </summary>
    OnCooldown
}

/// <summary>
/// Gerencia o estado atual do ciclo de ataque de uma entidade.
/// </summary>
public struct AttackState
{
    public AttackPhase Phase;
    
    /// <summary>
    /// Cronômetro usado tanto para a duração (casting) quanto para a recarga (cooldown).
    /// </summary>
    public float Timer;
}