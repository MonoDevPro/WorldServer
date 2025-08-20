using Simulation.Core.Commons;

namespace Simulation.Core.Components;

// Direção pretendida: vetor (ex.: (-1,0),(1,0),(0,1),(-1,-1), etc.)
public struct DirectionInput
{
    public VelocityVector Direction;
    
    public bool IsZero => Direction is { X: 0, Y: 0 };
}