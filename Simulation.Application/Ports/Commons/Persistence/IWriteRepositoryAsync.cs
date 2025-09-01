namespace Simulation.Application.Ports.Commons.Persistence;

public interface IWriteRepositoryAsync<TKey, TEntity>
    where TKey : notnull
    where TEntity : notnull
{
    Task AddAsync(TKey id, TEntity entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(TKey id, TEntity entity, CancellationToken ct = default);
    Task<bool> RemoveAsync(TKey id, CancellationToken ct = default);
}