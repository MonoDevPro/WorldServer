using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Utilities;

public sealed class CharIndex : ICharIndex
{
    // charId -> templateId
    private readonly Dictionary<int, CharTemplate> _charToTemplate = new();

    // name intern tables
    private readonly Dictionary<int, string> _nameById = new();
    private readonly Dictionary<string, int> _nameToId = new(StringComparer.Ordinal);

    /// <summary>Registra um novo template e retorna o templateId atribuído.</summary>
    public int RegisterTemplate(CharTemplate template)
    {
        // tenta evitar duplicates (opcional): procura por template igual
        if (_charToTemplate.TryGetValue(template.CharId.Value, out var existing))
            return existing.CharId.Value;

        _nameById[template.CharId.Value] = template.Name;
        _nameToId[template.Name] = template.CharId.Value;

        _charToTemplate[template.CharId.Value] = template;
        return template.CharId.Value;
    }

    public void DetachChar(int characterId)
    {
        if (_charToTemplate.Remove(characterId, out var template))
        {
            _nameById.Remove(template.CharId.Value);

            _nameToId.Remove(template.Name);
        }
    }

    public bool TryGetCharTemplate(int characterId, out CharTemplate? template)
        => _charToTemplate.TryGetValue(characterId, out template);

    public bool TryGetName(int charId, out string? name) 
        => _nameById.TryGetValue(charId, out name);
    
    public bool TryGetIdFromName(string name, out int charId)
        => _nameToId.TryGetValue(name, out charId);
}