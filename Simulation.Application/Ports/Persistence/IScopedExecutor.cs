namespace Simulation.Application.Ports.Persistence;

/// <summary>
/// Define um serviço que executa uma operação dentro de um escopo de injeção de dependência criado manualmente.
/// </summary>
public interface IScopedExecutor
{
    /// <summary>
    /// Cria um escopo, executa a ação fornecida e descarta o escopo.
    /// </summary>
    /// <param name="work">A ação a ser executada. Ela recebe o IServiceProvider do escopo criado.</param>
    Task ExecuteInScopeAsync(Func<IServiceProvider, Task> work);

    /// <summary>
    /// Cria um escopo, executa a função fornecida, retorna o resultado e descarta o escopo.
    /// </summary>
    Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> work);
}