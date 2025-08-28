namespace Simulation.Core.Abstractions.Commons;

// Tag Components
public struct Blocking { }

// Identificadores
public readonly record struct CharId(int Value);
public readonly record struct MapId(int Value);

// Estado de Entidade
public struct Position { public GameCoord Value; }
public struct Direction { public GameDirection Value; }
public struct Lifetime { public float RemainingSeconds; }

// Mapas
public readonly record struct MapSize(GameSize Value);
public readonly record struct MapFlags(bool UsePadded);
public readonly record struct MapLoadRequest(int MapId);

// Spatial Index
public readonly record struct SpatialDirty(GameCoord Old, GameCoord New);

// Movimento
public struct MoveStats { public float Speed; }
public struct MoveAction { public GameCoord Start; public GameCoord Target; public float Elapsed; public float Duration; }

// Ataque
public struct AttackStats { public float CastTime; public float Cooldown; }
public struct AttackAction { public float Duration; public float Remaining; public float Cooldown; public float CooldownRemaining; }

// Value Objects
public readonly record struct GameCoord(int X, int Y);       // Tile/grid
public readonly record struct GameDirection(float X, float Y); // Direção unitária
public readonly record struct GameVelocity(float X, float Y);  // Velocidade contínua
public readonly record struct GameSize(int Width, int Height);