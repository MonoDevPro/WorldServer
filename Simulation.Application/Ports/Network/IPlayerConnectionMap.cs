using Simulation.Application.Ports.Commons;

namespace Simulation.Application.Ports.Network;

public interface IPlayerConnectionMap : IIndex<int, int>, IReverseIndex<int, int>
{
    
}