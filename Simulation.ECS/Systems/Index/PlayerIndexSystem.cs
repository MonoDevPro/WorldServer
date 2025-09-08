using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Domain;

namespace Simulation.ECS.Systems.Index;

public sealed partial class PlayerIndexSystem(World world) : BaseSystem<World, float>(world)
{
    // O índice é privado e gerenciado inteiramente por este sistema
    private readonly Dictionary<int, Entity> _playersByCharId = new();

    [Query]
    [All<CharId>]
    [None<Indexed>]
    private void AddNewPlayers(ref Entity entity, ref CharId charId)
    {
        _playersByCharId[charId.Value] = entity;
        World.Add<Indexed>(entity); // Marca como indexado
    }

    public bool TryGetEntity(int charId, out Entity entity)
    {
        if (_playersByCharId.TryGetValue(charId, out entity))
        {
            // Verificação crucial: garante que não estamos retornando uma entidade "morta"
            // que já foi destruída mas ainda não foi removida do índice.
            if (World.IsAlive(entity))
            {
                return true;
            }
            
            // Auto-correção: remove a entidade morta do índice.
            _playersByCharId.Remove(charId);
        }
        entity = default;
        return false;
    }
}