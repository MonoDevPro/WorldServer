using Arch.Core;
using Microsoft.Extensions.Logging;

namespace Simulation.ECS;

/// <summary>
/// Serviço hospedado que executa o loop de simulação com timestep fixo e aplica comandos enfileirados.
/// </summary>
public class SimulationRunner(SimulationPipeline systems) : IDisposable
{
    public void Update(in float deltaTime)
    {
        systems.BeforeUpdate(in deltaTime);
        systems.Update(in deltaTime);
        systems.AfterUpdate(in deltaTime);
    }
    
    public void Dispose()
    {
        systems.Dispose();
    }
}
