using System.Runtime.CompilerServices;

namespace Simulation.Core.Abstractions.Commons;

public static class Extensions
{
    # region GameCoord Extensions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameCoord Sign(this GameCoord v) => new(Math.Sign(v.X), Math.Sign(v.Y));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameCoord Sum(this GameCoord v1, GameCoord v2) => new(v1.X + v2.X, v1.Y + v2.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameCoord Sub(this GameCoord v1, GameCoord v2) => new(v1.X - v2.X, v1.Y - v2.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameCoord Mul(this GameCoord v, float scalar) => new((int)(v.X * scalar), (int)(v.Y * scalar));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameCoord Div(this GameCoord v, float scalar) => new((int)(v.X / scalar), (int)(v.Y / scalar));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this GameCoord v) => v is { X: 0, Y: 0 };
    #endregion
    
    
}