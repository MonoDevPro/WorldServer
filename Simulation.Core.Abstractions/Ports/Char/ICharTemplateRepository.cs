using Simulation.Core.Abstractions.Adapters.Char;

namespace Simulation.Core.Abstractions.Ports.Char;

/// <summary>
/// Define um serviço para acessar os dados base (persistidos) dos personagens.
/// </summary>
public interface ICharTemplateRepository
{
    /// <summary>
    /// Obtém o template de um personagem a partir de uma fonte de dados (ex: banco de dados, arquivo).
    /// </summary>
    CharTemplate GetTemplate(int charId);

    /// <summary>
    /// Salva o estado de um personagem em uma fonte de dados.
    /// </summary>
    void SaveTemplate(CharTemplate template);
}
