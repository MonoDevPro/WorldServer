namespace Simulation.Application.Ports.Commons.Persistence;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);