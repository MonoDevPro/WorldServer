using Arch.Core;

namespace Simulation.Application.Ports.Commons.Factories;

public interface IQueryProvider<TTemplate>
{
    QueryDescription GetQueryDescription();
}