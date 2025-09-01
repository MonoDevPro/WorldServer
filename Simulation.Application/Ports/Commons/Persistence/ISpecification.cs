namespace Simulation.Application.Ports.Commons.Persistence;

public interface ISpecification<T>
{
    // Se você usa IQueryable há implementações que retornam Expression<Func<T,bool>>
    // Aqui deixamos abstrato para ser implementado por cada infra.
    bool IsSatisfiedBy(T entity);
}