using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Char;

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
