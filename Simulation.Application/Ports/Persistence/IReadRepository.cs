namespace Simulation.Application.Ports.Persistence;

public interface IReadRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : notnull
{
    TEntity? Get(TKey id);                // retorna null se não existir
    bool TryGet(TKey id, out TEntity? e); // Try pattern quando preferir evitar exceções
    IEnumerable<TEntity> GetAll();
    PagedResult<TEntity> GetPage(int page, int pageSize);
    IEnumerable<TEntity> Find(ISpecification<TEntity> spec);
}