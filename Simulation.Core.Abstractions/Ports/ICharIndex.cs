using Simulation.Core.Abstractions.Adapters.Data;

namespace Simulation.Core.Abstractions.Ports;

public interface ICharIndex
{
    int RegisterTemplate(CharTemplate template);
    void DetachChar(int characterId);
    bool TryGetCharTemplate(int characterId, out CharTemplate? template);
    bool TryGetName(int nameId, out string? name);
    bool TryGetIdFromName(string name, out int charId);
}