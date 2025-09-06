namespace GameClient.Scripts.Domain;

// Minimal subset of server components required for client prediction/rendering
public struct CharId { public int Value; }
public struct MapId { public int Value; }
public struct Position { public int X, Y; }
public struct Direction { public int X, Y; }
public struct MoveStats { public float Speed; }
public struct AttackStats { public float CastTime; public float Cooldown; }

// Tag to mark player controlled entity
public struct LocalPlayer { }

// Client-side only interpolation state
public struct Interpolation
{
	public float StartX, StartY;
	public float TargetX, TargetY;
	public float CurrentX, CurrentY;
	public float Duration; // seconds
	public float Elapsed;  // seconds
}
