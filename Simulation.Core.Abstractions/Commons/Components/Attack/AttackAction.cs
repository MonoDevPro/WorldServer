namespace Simulation.Core.Abstractions.Commons.Components.Attack;

public struct AttackAction
{
    /// <summary>Tempo total da animação/efeito (segundos) — duração planejada.</summary>
    public float Duration;

    /// <summary>Tempo restante até a ação terminar (segundos).</summary>
    public float Remaining;

    /// <summary>Cooldown total após terminar (segundos).</summary>
    public float Cooldown;

    /// <summary>Tempo restante de cooldown (segundos). Quando > 0, a ação não pode ser iniciada.</summary>
    public float CooldownRemaining;
    
    public bool IsOnCooldown => CooldownRemaining > 0f;
}