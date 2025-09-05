using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.DTOs;

// Character Snapshots
public record struct EnterSnapshot(int MapId, int CharId, CharTemplate[] templates);
public record struct CharSnapshot(int MapId, int CharId, CharTemplate Template);
public record struct ExitSnapshot(int MapId, int CharId, CharTemplate Template);
public record struct AttackSnapshot(int CharId);
public record struct MoveSnapshot(int CharId, Position Old, Position New);
public record struct TeleportSnapshot(int CharId, int MapId, Position Position);
