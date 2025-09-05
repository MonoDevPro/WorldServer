namespace Simulation.Application.Ports.Commons.Factories;

public interface IFactory<TEntity, TTemplate>
{
    TEntity Create(TTemplate data);
}