using Simulation.Application.DTOs;

namespace Simulation.Application.Ports.Map;

public interface IMapIntentHandler
{
    void HandleIntent(in LoadMapIntent intent);
    void HandleIntent(in UnloadMapIntent intent);
}