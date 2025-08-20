using System.Runtime.CompilerServices;

namespace Simulation.Core.Commons;

/// <summary>
/// Representa uma posição ou direção no grid lógico do jogo.
/// </summary>
public readonly record struct GameVector2(int X, int Y)
{
    public static readonly GameVector2 Zero = new(0, 0);
    public static readonly GameVector2 North = new (0, -1);
    public static readonly GameVector2 South = new (0, 1);
    public static readonly GameVector2 East = new (1, 0);
    public static readonly GameVector2 West = new (-1, 0);
    public static readonly GameVector2 NorthWest = new (-1, -1);
    public static readonly GameVector2 NorthEast = new (1, -1);
    public static readonly GameVector2 SouthWest = new (-1, 1);
    public static readonly GameVector2 SouthEast = new (1, 1);
    
    public bool IsZero => X == 0 && Y == 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GameVector2 Sign()
        => new(Math.Sign(X), Math.Sign(Y));
    
    /// <summary>
    /// Retorna o quadrado da distância Euclidiana. Mais rápido para comparações.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceSquaredTo(GameVector2 other)
    {
        int dx = other.X - X;
        int dy = other.Y - Y;
        return dx * dx + dy * dy;
    }
    
    /// <summary>
    /// Retorna a distância Euclidiana até outro ponto no grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceTo(GameVector2 other)
    {
        return (int)Math.Sqrt(DistanceSquaredTo(other));
    }
    
    public static GameVector2 operator +(GameVector2 a, GameVector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static GameVector2 operator -(GameVector2 a, GameVector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static GameVector2 operator *(GameVector2 a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static VelocityVector operator *(GameVector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static GameVector2 operator /(GameVector2 a, int scalar) => new(a.X / scalar, a.Y / scalar);
    
    public bool Equals(GameVector2 other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"Grid({X}, {Y})";
    
    public static GameVector2 FromString(string s) 
    {
        var parts = s.Trim("Grid()".ToCharArray()).Split(',');
        if (parts.Length != 2) throw new FormatException("Invalid MapPosition format");
        return new GameVector2(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}