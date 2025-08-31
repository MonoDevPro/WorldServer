using Arch.Buffer;
using Arch.Core;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Char;

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
        Component<Blocking>.ComponentType
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

    public static CharTemplate CreateCharTemplate(World world, in Entity entity)
    {
        // Directly get references and build the new template
        ref var cid = ref world.Get<CharId>(entity);
        ref var mid = ref world.Get<MapId>(entity);
        ref var pos = ref world.Get<Position>(entity);
        ref var dir = ref world.Get<Direction>(entity);
        ref var mv = ref world.Get<MoveStats>(entity);
        ref var atk = ref world.Get<AttackStats>(entity);

        return new CharTemplate
        {
            CharId = cid.Value,
            MapId = mid.Value,
            Position = pos,
            Direction = dir,
            MoveSpeed = mv.Speed,
            AttackCastTime = atk.CastTime,
            AttackCooldown = atk.Cooldown,
        };
    }
}