using Simulation.Core.Commons.Enums;

namespace Simulation.Core.Components;

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
    
    /// <summary>
    /// Flag interna para evitar que o primeiro tick de cooldown desconte dt no mesmo frame da transição.
    /// </summary>
    public bool EnteredCooldownThisFrame;
}