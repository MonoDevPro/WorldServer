using System.Globalization;
using System.Runtime.CompilerServices;

namespace Simulation.Core.Commons;

public readonly record struct VelocityVector(float X, float Y)
{
    private const float ZeroToleranceSq = 1e-12f; // tolerância em comprimento²

    public bool IsZero
        => LengthSquared() <= ZeroToleranceSq;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelocityVector Normalize()
    {
        var lenSq = LengthSquared();
        if (lenSq <= ZeroToleranceSq)
            return Zero;
        var invLen = 1f / MathF.Sqrt(lenSq);
        return new VelocityVector(X * invLen, Y * invLen);
    }

    /// <summary>Retorna vetor na mesma direção com comprimento == targetLength (se não zero).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelocityVector Normalize(float targetLength)
    {
        if (targetLength <= 0f)
            return Zero;
        var lenSq = LengthSquared();
        if (lenSq <= ZeroToleranceSq)
            return Zero;
        var scale = targetLength / MathF.Sqrt(lenSq);
        return new VelocityVector(X * scale, Y * scale);
    }

    /// <summary>Garante que o comprimento fique no intervalo [minLength, maxLength].</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VelocityVector Clamp(float minLength, float maxLength)
    {
        if (minLength < 0f) minLength = 0f;
        if (maxLength < minLength) maxLength = minLength;

        var lenSq = LengthSquared();
        if (lenSq <= ZeroToleranceSq)
            return Zero; // permanece zero

        var minSq = minLength * minLength;
        var maxSq = maxLength * maxLength;

        if (lenSq < minSq)
            return Normalize(minLength);
        if (lenSq > maxSq)
            return Normalize(maxLength);
        return this;
    }
    
    public GameVector2 Sign()
        => new(Math.Sign(X), Math.Sign(Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
        => MathF.Sqrt(LengthSquared());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
        => X * X + Y * Y;

    public static readonly VelocityVector Zero = new(0f, 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelocityVector operator +(VelocityVector a, VelocityVector b)
        => new(a.X + b.X, a.Y + b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelocityVector operator -(VelocityVector a, VelocityVector b)
        => new(a.X - b.X, a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelocityVector operator *(VelocityVector a, float scalar)
        => new(a.X * scalar, a.Y * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VelocityVector operator /(VelocityVector a, float scalar)
        => new(a.X / scalar, a.Y / scalar);

    public static float Dot(in VelocityVector a, in VelocityVector b)
        => a.X * b.X + a.Y * b.Y;

    public override string ToString()
        => $"Vel({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)})";

    public static VelocityVector Parse(string s)
    {
        // Aceita formatos Vel(x, y) ou Grid(x, y)
        var trimmed = s.Trim();
        int open = trimmed.IndexOf('(');
        int close = trimmed.IndexOf(')');
        if (open < 0 || close <= open)
            throw new FormatException("Formato inválido");
        var inner = trimmed.Substring(open + 1, close - open - 1);
        var parts = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new FormatException("Formato inválido");
        return new VelocityVector(
            float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture)
        );
    }
}