namespace Simulation.Application.DTOs.Snapshots;

public record JoinAckDto(
    int YourCharId,
    int YourEntityId,
    int MapId,
    IReadOnlyList<PlayerStateDto> Others
);