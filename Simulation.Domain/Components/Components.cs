
namespace Simulation.Domain.Components;

// Tag Components
public struct Blocking { }

// Identificadores
public readonly record struct CharId(int Value);
public readonly record struct MapId(int Value);

// Estado de Entidade
public struct Position { public int X,Y; }
public struct Direction { public int X,Y; }
public struct Lifetime { public float RemainingSeconds; }

// Mapas
public readonly record struct MapSize(int Width, int Height);
public readonly record struct MapFlags(bool UsePadded);
public readonly record struct MapLoadRequest(int MapId);

// Spatial Index
public readonly record struct SpatialDirty(int X, int Y);

// Movimento
public struct MoveStats { public float Speed; }
public struct MoveAction { public Position Start,Target; public float Elapsed, Duration; }

// Ataque
public struct AttackStats { public float CastTime; public float Cooldown; }
public struct AttackAction { public float Duration, Remaining, Cooldown, CooldownRemaining; }

// Input
public struct Input { public int X,Y; }