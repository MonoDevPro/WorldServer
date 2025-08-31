using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Map;

/// <summary>
/// Define um contrato para um serviço que carrega dados de mapa e os prepara para a simulação.
/// Esta é a "Porta" na arquitetura Ports & Adapters.
/// </summary>
public interface IMapLoaderService
{
    /// <summary>
    /// Carrega um mapa de forma síncrona, enfileirando-o para processamento no mundo ECS.
    /// </summary>
    /// <param name="mapId">O ID do mapa a ser carregado.</param>
    /// <returns>Verdadeiro se o mapa foi enfileirado com sucesso.</returns>
    bool LoadMap(int mapId);

    /// <summary>
    /// Carrega um mapa de forma assíncrona, enfileirando-o para processamento no mundo ECS.
    /// </summary>
    /// <param name="mapId">O ID do mapa a ser carregado.</param>
    /// <param name="ct">Um token de cancelamento.</param>
    /// <returns>Verdadeiro se o mapa foi enfileirado com sucesso.</returns>
    Task<bool> LoadMapAsync(int mapId, CancellationToken ct = default);
    
    // NOTA: Os métodos de "Salvar" poderiam pertencer a outra interface (ex: IMapRepository)
    // para uma segregação de responsabilidades ainda mais estrita, mas incluí-los aqui
    // é aceitável para simplificar.
    
    /// <summary>
    /// Salva um template de mapa para a fonte de dados.
    /// </summary>
    void SaveMapTemplateToFile(MapTemplate template);

    /// <summary>
    /// Salva um template de mapa de forma assíncrona.
    /// </summary>
    Task SaveMapTemplateToFileAsync(MapTemplate template, CancellationToken ct = default);
}
