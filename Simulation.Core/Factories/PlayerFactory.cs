using Arch.Core;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Factories;

public static class PlayerFactory
{
    public static Entity Create(World world, int characterId, int mapId, GameVector2 initialPosition)
    {
        return world.Create(
            new CharId { CharacterId = characterId },
            new MapRef { MapId = mapId }, // O MapId pode ser um parâmetro
            new TilePosition { Position = initialPosition },
            new TileVelocity(),
            new MoveAccumulator(),
            new MoveSpeed { Value = 5.0f }, // Valor padrão de velocidade
            new AttackSpeed { CastTime = 0.5f, Cooldown = 1.5f }
        );
    }
}