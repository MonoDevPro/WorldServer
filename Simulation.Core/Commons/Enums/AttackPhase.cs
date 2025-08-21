namespace Simulation.Core.Commons.Enums;

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