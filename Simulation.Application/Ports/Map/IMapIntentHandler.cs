using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Map;

public interface IMapIntentHandler
{
    void HandleIntent(in LoadMapIntent intent, MapTemplate data);
    void HandleIntent(in UnloadMapIntent intent);
}