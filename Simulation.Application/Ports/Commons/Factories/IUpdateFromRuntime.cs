using Arch.Core;

namespace Simulation.Application.Ports.Commons.Factories;

/// <summary>
/// Define um contrato para classes que podem atualizar um modelo de dados
/// a partir do estado de tempo de execução de uma entidade.
/// </summary>
/// <typeparam name="TModel">O tipo do modelo de dados a ser atualizado (ex: CharTemplate).</typeparam>
/// <typeparam name="TRuntimeEntity">O tipo da entidade de tempo de execução (ex: Entity).</typeparam>
public interface IUpdateFromRuntime<in TModel, in TRuntimeEntity>
{
    /// <summary>
    /// Atualiza um modelo de dados com as informações de uma entidade em tempo de execução.
    /// </summary>
    /// <param name="model">O modelo a ser atualizado.</param>
    /// <param name="runtimeEntity">A entidade de onde os dados serão lidos.</param>
    /// <param name="world">O mundo ECS onde a entidade existe.</param>
    void UpdateFromRuntime(TModel model, TRuntimeEntity runtimeEntity, World world);
}