using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Ports.Index;

namespace Simulation.Core.Abstractions.Adapters.Index;

/// <summary>
/// Implementação em memória do ICharTemplateRepository.
/// Simula um banco de dados carregando templates pré-definidos.
/// </summary>
public class InMemoryCharTemplateRepository : ICharTemplateRepository
{
    // Este dicionário simula nossa tabela de "personagens" no banco de dados.
    private readonly Dictionary<int, CharTemplate> _templates = new();

    public InMemoryCharTemplateRepository()
    {
        // Pré-carrega alguns personagens para teste. Em um cenário real,
        // isso viria de um arquivo de configuração ou de uma consulta ao banco de dados.
        _templates[1] = new CharTemplate { CharId = 123, Name = "Filipe", MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f };
        _templates[1] = new CharTemplate { CharId = 123, Name = "Rodorfo", MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f };
    }

    public CharTemplate GetTemplate(int charId)
    {
        // Em um sistema real, aqui você faria: "SELECT * FROM Characters WHERE Id = @charId"
        if (_templates.TryGetValue(charId, out var template))
        {
            // Retorna uma cópia para evitar que a simulação modifique o template original diretamente.
            return template;
        }

        // Se o personagem não existe, cria um padrão para evitar crashes.
        return new CharTemplate { CharId = charId, Name = $"Guest_{charId}", MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f };
    }

    public void SaveTemplate(CharTemplate template)
    {
        // Em um sistema real, aqui você faria: "UPDATE Characters SET ... WHERE Id = @charId"
        _templates[template.CharId] = template;
        Console.WriteLine($"[Repository] Personagem {template.Name} (ID: {template.CharId}) salvo.");
    }
}
