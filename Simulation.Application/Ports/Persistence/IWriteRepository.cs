namespace Simulation.Application.Ports.Persistence;

public interface IWriteRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : notnull
{
    void Add(TKey id, TEntity entity);   // lança ao duplicar (ou escolha Upsert)
    bool Update(TKey id, TEntity entity); // bool para sucesso/falha
    bool Remove(TKey id);
}