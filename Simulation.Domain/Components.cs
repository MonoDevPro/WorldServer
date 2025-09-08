using Arch.Core;

namespace Simulation.Domain;


// Indexadores
public struct Indexed;
public struct MapIndexed;
public struct InSpatialIndex;
public struct LastKnownPosition { public Position Value; }

// Identificadores
public struct CharId { public int Value; }
public struct MapId { public int Value; }

// Combate
public struct AttackIntent { public Entity Target; }
public struct AttackStats { public float CastTime; public float Cooldown; public int Damage; public int AttackRange; }
public struct AttackAction { public Entity Target; public float CastTimeRemaining; }
public struct AttackCooldown { public float CooldownRemaining; }
public struct Health { public int Current; public int Max; }
public struct Dead;

// Movimentação
public struct MoveIntent { public Direction Directioon; }
public struct MoveStats { public float Speed; }
public struct MoveAction { public Position Start,Target; public float Elapsed, Duration; }

// Teletransporte
public struct TeleportIntent { public Position TargetPosition; }
public struct TeleportCooldown { public float CooldownRemaining; }

// Estado de Entidade
public struct Position { public int X,Y; }
public struct Direction { public int X, Y; }
