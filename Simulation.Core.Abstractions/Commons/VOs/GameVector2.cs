using System.Globalization;
using System.Runtime.CompilerServices;

namespace Simulation.Core.Abstractions.Commons;

/// <summary>
/// Representa uma posição ou direção no grid lógico do jogo (inteiro / tile-based).
/// </summary>
public readonly record struct GameVector2(int X, int Y)
{
    public static readonly GameVector2 Zero = new(0, 0);
    public static readonly GameVector2 North = new(0, -1);
    public static readonly GameVector2 South = new(0, 1);
    public static readonly GameVector2 East  = new(1, 0);
    public static readonly GameVector2 West  = new(-1, 0);
    public static readonly GameVector2 NorthWest = new(-1, -1);
    public static readonly GameVector2 NorthEast = new(1, -1);
    public static readonly GameVector2 SouthWest = new(-1, 1);
    public static readonly GameVector2 SouthEast = new(1, 1);

    private const float EPS = 1e-6f;

    public bool IsZero => X == 0 && Y == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly GameVector2 Sign()
        => new(Math.Sign(X), Math.Sign(Y));

    /// <summary>
    /// Retorna o quadrado da distância Euclidiana (usado para comparações).
    /// Note: usa long para reduzir risco de overflow em mapas grandes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly long DistanceSquaredTo(in GameVector2 other)
    {
        long dx = (long)other.X - X;
        long dy = (long)other.Y - Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Distância Euclidiana como float (mantém precisão).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float DistanceTo(in GameVector2 other)
    {
        return MathF.Sqrt((float)DistanceSquaredTo(other));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameVector2 operator +(GameVector2 a, GameVector2 b) => new(a.X + b.X, a.Y + b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameVector2 operator -(GameVector2 a, GameVector2 b) => new(a.X - b.X, a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameVector2 operator *(GameVector2 a, int scalar) => new(a.X * scalar, a.Y * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameVector2 operator /(GameVector2 a, int scalar) => new(a.X / scalar, a.Y / scalar);

    // Conveniência: conversão para VelocityVector (supondo componente float)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly VelocityVector ToVelocityVector(float scalar) => new(X * scalar, Y * scalar);

    // Se quiser manter operador direto (opcional)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelocityVector operator *(GameVector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);

    public readonly bool Equals(GameVector2 other) => X == other.X && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"Grid({X},{Y})";

    /// <summary>
    /// Tenta parsear "Grid(x,y)" ou "x,y" (tolerante a espaços).
    /// </summary>
    public static bool TryParse(string s, out GameVector2 result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        // Remove prefix/sufix "Grid(" ")" se houver
        s = s.Trim();
        if (s.StartsWith("Grid(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            s = s.Substring(5, s.Length - 6);

        var parts = s.Split(',');
        if (parts.Length != 2) return false;

        if (int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
            int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
        {
            result = new GameVector2(x, y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Lança FormatException em falha — útil se você quiser comportamento exception-based.
    /// </summary>
    public static GameVector2 Parse(string s)
    {
        if (TryParse(s, out var v)) return v;
        throw new FormatException("Invalid GameVector2 format. Expected 'Grid(x,y)' or 'x,y'.");
    }
}
