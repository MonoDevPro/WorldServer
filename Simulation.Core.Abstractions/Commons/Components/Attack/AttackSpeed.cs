namespace Simulation.Core.Abstractions.Commons.Components.Attack;

/// <summary>
/// Define os atributos de velocidade do ataque básico de uma entidade.
/// </summary>
public struct AttackSpeed
{
    /// <summary>
    /// O tempo em segundos para preparar o ataque antes que ele cause efeito (cast time).
    /// </summary>
    public float CastTime;

    /// <summary>
    /// O tempo total em segundos que a entidade deve esperar antes de poder iniciar um novo ataque.
    /// </summary>
    public float Cooldown;
}