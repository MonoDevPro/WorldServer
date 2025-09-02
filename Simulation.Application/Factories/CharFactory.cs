using Arch.Buffer;
using Arch.Core;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Factories;

public static class CharFactory
{
    // 1. DEFINIÇÃO CENTRAL DO ARQUÉTIPO DO PERSONAGEM
    // Esta é agora a única fonte da verdade para a estrutura de um personagem.
    private static readonly ComponentType[] CharacterArchetype =
    {
        Component<CharId>.ComponentType, 
        Component<MapId>.ComponentType, 
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType, 
        Component<MoveStats>.ComponentType, 
        Component<AttackStats>.ComponentType,
        Component<Blocking>.ComponentType,
        Component<Version>.ComponentType
    };
    private static readonly Signature CharacterSignature = SignatureBuilder.Create(CharacterArchetype);
    
    // A QueryDescription agora usa a definição central
    public static QueryDescription QueryDescription = new(all: CharacterSignature);
    
    // 2. HELPER CENTRALIZADO PARA POPULAR OS DADOS
    // Este método contém a lógica de atribuição de dados e é reutilizado abaixo.
    private static void PopulateEntityFromTemplate(Entity entity, CharTemplate tpl, CommandBuffer cmd)
    {
        cmd.Set(entity, new CharId { Value = tpl.CharId });
        cmd.Set(entity, new MapId { Value = tpl.MapId });
        cmd.Set(entity, tpl.Position);
        cmd.Set(entity, tpl.Direction);
        cmd.Set(entity, new MoveStats { Speed = tpl.MoveSpeed });
        cmd.Set(entity, new AttackStats
        {
            CastTime = tpl.AttackCastTime,
            Cooldown = tpl.AttackCooldown
        });
        cmd.Set(entity, new Blocking());
        cmd.Set(entity, new Version { Value = 0 }); // Inicia a versão em 0
    }
    
    // 3. MÉTODOS PÚBLICOS REATORADOS (MAIS LIMPOS E SEM REPETIÇÃO)
    
    /// <summary>
    /// Cria uma entidade agendada em um CommandBuffer a partir de um template.
    /// </summary>
    public static Entity CreateEntity(CommandBuffer cmd, CharTemplate tpl)
    {
        // Usa o arquétipo centralizado para definir a estrutura
        var entity = cmd.Create(CharacterArchetype);

        // Usa o helper centralizado para agendar a atribuição dos dados
        PopulateEntityFromTemplate(entity, tpl, cmd);

        return entity;
    }
    
    public static CharTemplate UpdateCharTemplate(World world, in Entity entity, CharTemplate tpl)
    {
        // Atualiza o template existente com os dados atuais da entidade
        ref var cid = ref world.Get<CharId>(entity);
        ref var mid = ref world.Get<MapId>(entity);
        ref var pos = ref world.Get<Position>(entity);
        ref var dir = ref world.Get<Direction>(entity);
        ref var mv = ref world.Get<MoveStats>(entity);
        ref var atk = ref world.Get<AttackStats>(entity);

        tpl.CharId = cid.Value;
        tpl.MapId = mid.Value;
        tpl.Position = pos;
        tpl.Direction = dir;
        tpl.MoveSpeed = mv.Speed;
        tpl.AttackCastTime = atk.CastTime;
        tpl.AttackCooldown = atk.Cooldown;

        return tpl;
    }

    public static CharTemplate CreateCharTemplate(World world, in Entity entity)
    {
        var template = new CharTemplate();
        return UpdateCharTemplate(world, entity, template);
    }
}