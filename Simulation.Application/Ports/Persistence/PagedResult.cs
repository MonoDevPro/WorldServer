namespace Simulation.Application.Ports.Persistence;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);