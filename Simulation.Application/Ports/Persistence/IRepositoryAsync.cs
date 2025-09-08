namespace Simulation.Application.Ports.Persistence;

public interface IRepositoryAsync<TKey, TEntity> :
    IReadRepositoryAsync<TKey, TEntity>,
    IWriteRepositoryAsync<TKey, TEntity>
    where TKey : notnull
    where TEntity : notnull { }