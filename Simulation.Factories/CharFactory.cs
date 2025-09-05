using Arch.Buffer;
using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char;
using Simulation.Application.Services;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Factories;

// A fábrica agora implementa interfaces mais específicas e semânticas.
public class CharFactory(DoubleBufferedCommandBuffer buffer) : ICharFactory
{
    private static readonly ComponentType[] ArchetypeComponents =
    [
        Component<CharId>.ComponentType,
        Component<MapId>.ComponentType, 
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType, 
        Component<MoveStats>.ComponentType, 
        Component<AttackStats>.ComponentType,
        Component<Blocking>.ComponentType,
    ];
    private static readonly QueryDescription QueryDesc = new(all: SignatureBuilder.Create(components: ArchetypeComponents));

    public static Entity Create(DoubleBufferedCommandBuffer cmd, CharTemplate data)
    {
        var entity = cmd.Create(ArchetypeComponents);
        
        cmd.Set(entity, new CharId { Value = data.CharId });
        cmd.Set(entity, new MapId { Value = data.MapId });
        cmd.Set(entity, data.Position);
        cmd.Set(entity, data.Direction);
        cmd.Set(entity, new MoveStats { Speed = data.MoveSpeed });
        cmd.Set(entity, new AttackStats
        {
            CastTime = data.AttackCastTime,
            Cooldown = data.AttackCooldown
        });
        cmd.Set(entity, new Blocking());
        return entity;
    }
    
    public Entity Create(CharTemplate data)
    {
        return Create(buffer, data);
    }
    
    /// <summary>
    /// (NOVO) Atualiza um CharTemplate com os dados atuais de uma entidade do jogo.
    /// Este método é a ponte para salvar o estado do jogador.
    /// </summary>
    public void UpdateFromRuntime(CharTemplate model, Entity entity, World world)
    {
        if (!world.IsAlive(entity)) return;

        // Mapeia os dados dos componentes de volta para o modelo de dados (template)
        model.MapId = world.Get<MapId>(entity).Value;
        model.Position = world.Get<Position>(entity);
        model.Direction = world.Get<Direction>(entity);
        
        var moveStats = world.Get<MoveStats>(entity);
        model.MoveSpeed = moveStats.Speed;
        
        var attackStats = world.Get<AttackStats>(entity);
        model.AttackCastTime = attackStats.CastTime;
        model.AttackCooldown = attackStats.Cooldown;
        
        // Outros componentes que representam o estado (vida, mana, xp, inventário)
        // seriam lidos da entidade e atualizados no modelo aqui também.
    }
    
    public void UpdateFromRuntime(CharSaveTemplate model, Entity e, World world)
    {
        if (!world.IsAlive(e)) return;

        model.CharId = world.Get<CharId>(e);
        model.MapId = world.Get<MapId>(e);
        model.Position = world.Get<Position>(e);
        model.Direction = world.Get<Direction>(e);
        model.MoveStats = world.Get<MoveStats>(e);
        model.AttackStats = world.Get<AttackStats>(e);
    }
    
    public ComponentType[] GetArchetypeComponents()
    {
        return ArchetypeComponents;
    }

    public QueryDescription GetQueryDescription()
    {
        return QueryDesc;
    }
}