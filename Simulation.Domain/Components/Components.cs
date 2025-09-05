
namespace Simulation.Domain.Components;

// Tag Components
public struct Blocking { }
public struct SpatialDirty { }
public struct InCombat { }

// Identificadores
public struct CharId { public int Value; }
public struct MapId { public int Value; }

// Estado de Entidade
public struct Position { public int X,Y; }
public struct Direction { public int X, Y; }
public struct Lifetime { public float RemainingSeconds; }

// Mapas
public struct MapSize{ public int Width, Height;}
public struct MapFlags {public bool UsePadded; };

// Movimento
public struct MoveStats { public float Speed; }
public struct MoveAction { public Position Start,Target; public float Elapsed, Duration; }

// Ataque
public struct AttackStats { public float CastTime; public float Cooldown; }
public struct AttackAction { public float Duration, Remaining, Cooldown, CooldownRemaining; }

// Input
public struct Input { public int X,Y; }

// Teleporte
public struct TeleportCooldown { public float Remaining; }