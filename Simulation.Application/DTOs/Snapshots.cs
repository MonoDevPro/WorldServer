using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.DTOs;

public record struct EnterSnapshot(int mapId, int charId, CharTemplate[] templates);
public record struct CharSnapshot(int MapId, int CharId, CharTemplate Template);
public record struct ExitSnapshot(int CharId);
public record struct AttackSnapshot(int CharId);
public record struct MoveSnapshot(int CharId, Position OldPosition, Position NewPosition);