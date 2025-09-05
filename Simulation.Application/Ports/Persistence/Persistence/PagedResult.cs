namespace Simulation.Application.Ports.Persistence.Persistence;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);