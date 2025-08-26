using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Commons.Components.Move;

/// <summary>
/// Estado transitório de movimento: existe apenas enquanto a entidade está se deslocando de uma tile para outra.
/// </summary>
public struct MoveState
{
    public GameVector2 Start;   // tile de origem (inteiro)
    public GameVector2 Target;  // tile destino (inteiro)
    public float Elapsed;       // tempo acumulado desde o início do movimento
    public float Duration;      // tempo total para completar o movimento (segundos)
}