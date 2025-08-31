using System.Runtime.CompilerServices;
using Simulation.Domain.Components;

namespace Simulation.Domain.Helpers;

public static class ComponentsHelper
{
    # region Position Extensions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Position Sign(this Position v) { v.X = Math.Sign(v.X); v.Y = Math.Sign(v.Y); return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Position Sum(this Position v1, Position v2) { v1.X += v2.X; v1.Y += v2.Y; return v1; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Position Sub(this Position v1, Position v2) { v1.X -= v2.X; v1.Y -= v2.Y; return v1; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Position Mul(this Position v, float scalar) { v.X = (int)(v.X * scalar); v.Y = (int)(v.Y * scalar); return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Position Div(this Position v, float scalar) { v.X = (int)(v.X / scalar); v.Y = (int)(v.Y / scalar); return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this Position v) => v is { X: 0, Y: 0 };
    #endregion
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this Direction v) => v is { X: 0, Y: 0 };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this Input v) => v is { X: 0, Y: 0 };
    
}