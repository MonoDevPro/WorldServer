using Simulation.Domain.Components;

namespace Simulation.Application.DTOs;

public record struct EnterIntent(int CharId);
public record struct ExitIntent(int CharId);
public record struct AttackIntent(int AttackerCharId);
public record struct MoveIntent(int CharId, Input Input);
public record struct TeleportIntent(int CharId, int TargetMapId, Position TargetPos);
public record struct TeleportSnapshot(int CharId, int MapId, Position Position);