namespace Simulation.Application.Ports.Commons.Persistence;

public interface IReadRepositoryAsync<TKey, TEntity>
    where TKey : notnull
    where TEntity : notnull
{
    Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default);
    Task<(bool Found, TEntity? Entity)> TryGetAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<TEntity>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
}