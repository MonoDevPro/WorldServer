using Microsoft.Extensions.ObjectPool;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

/// <summary>
/// Uma política de pool específica para objetos CharTemplate.
/// A responsabilidade desta política é resetar um CharTemplate para um
/// estado limpo e previsível quando ele é devolvido ao pool.
/// </summary>
public class CharTemplatePolicy : IPooledObjectPolicy<CharTemplate>
{
    public CharTemplate Create() => new();

    public bool Return(CharTemplate template)
    {
        // Reseta manualmente todos os campos para seus valores padrão.
        template.Name = string.Empty;
        template.Gender = Gender.None;
        template.Vocation = Vocation.None;
        template.CharId = 0;
        template.MapId = 0;
        template.Position = default;
        template.Direction = default;
        template.MoveSpeed = 0f;
        template.AttackCastTime = 0f;
        template.AttackCooldown = 0f;
        
        return true;
    }
}
