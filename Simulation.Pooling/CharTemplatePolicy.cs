using Microsoft.Extensions.ObjectPool;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

/// <summary>
/// Uma política de pool específica para objetos CharTemplate.
/// A responsabilidade desta política é resetar um CharTemplate para um
/// estado limpo e previsível quando ele é devolvido ao pool.
/// </summary>
/// <summary>
/// Pooled policy para CharTemplate — reseta o objeto ao retorná-lo.
/// </summary>
public sealed class CharTemplatePooledPolicy : PooledObjectPolicy<PlayerTemplate>
{
    public override PlayerTemplate Create() => new PlayerTemplate();

    public override bool Return(PlayerTemplate obj)
    {
        Reset(obj);
        return true;
    }

    private static void Reset(PlayerTemplate t)
    {
        // Ajuste aqui de acordo com as propriedades reais de CharTemplate.
        // Exemplo conforme seu snippet original:
        t.Name = string.Empty;
        t.Gender = default;
        t.Vocation = default;
        t.CharId = default;
        t.MapId = default;
        t.Position = default;
        t.Direction = default;
        t.MoveSpeed = default;
        t.AttackCastTime = default;
        t.AttackCooldown = default;
    }
}