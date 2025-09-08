using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.Persistence;

namespace Simulation.Persistence.Utils;

public class ScopedExecutor : IScopedExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScopedExecutor> _logger;

    public ScopedExecutor(IServiceProvider serviceProvider, ILogger<ScopedExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> work)
    {
        // Cria o escopo, que garante que todos os serviços Scoped (DbContext, etc.)
        // sejam descartados ao final do bloco 'using'.
        using (var scope = _serviceProvider.CreateScope())
        {
            try
            {
                // Executa a ação passando o provedor de serviços do escopo.
                await work(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu uma exceção dentro de uma execução com escopo.");
                throw; // Re-lança a exceção para que o chamador saiba que algo deu errado.
            }
        }
    }
    
    public async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            try
            {
                return await work(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu uma exceção dentro de uma execução com escopo que retornaria um valor.");
                throw;
            }
        }
    }
}