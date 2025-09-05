using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.ECS.Handlers;

public interface IMapIntentHandler
{
    void HandleIntent(in LoadMapIntent intent, MapTemplate data);
    void HandleIntent(in UnloadMapIntent intent);
}