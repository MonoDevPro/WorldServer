namespace Simulation.Application.Ports.Persistence.Persistence;

public interface IRepository<TKey, TEntity> :
    IReadRepository<TKey, TEntity>,
    IWriteRepository<TKey, TEntity>,
    IDisposable
    where TKey : notnull
    where TEntity : notnull { }