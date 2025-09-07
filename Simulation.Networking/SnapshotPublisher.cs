using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Ports.ECS.Publishers;
using Simulation.Application.Ports.Network;
using Simulation.Application.Ports.Network.Domain.Enums;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Networking.Packets;

namespace Simulation.Networking;

/// <summary>
/// Implementação do publicador de snapshots que converte DTOs de simulação em pacotes de rede e os envia.
/// Atua como um Adaptador entre o Core da Simulação e o Core da Rede.
/// </summary>
public class SnapshotPublisher : IPlayerSnapshotPublisher
{
    private readonly IPacketSender _packetSender;
    private readonly IPlayerConnectionMap _mapping;
    private readonly ILogger<SnapshotPublisher> _logger;

    public SnapshotPublisher(IPacketSender packetSender, IPlayerConnectionMap mapping, ILogger<SnapshotPublisher> logger)
    {
        _packetSender = packetSender;
        _mapping = mapping;
        _logger = logger;
    }

    /// <summary>
    /// Publica o snapshot de confirmação de entrada para um jogador específico.
    /// </summary>
    /// <param name="peerId">O ID de conexão do jogador que receberá a confirmação.</param>
    /// <param name="joinAck">O snapshot com os dados do jogador e do mundo.</param>
    public void Publish(in JoinAckSnapshot joinAck)
    {
        if (_mapping.TryGet(joinAck.YourCharId, out int peerId))
        {
            var others = new List<PlayerStateDto>();
            foreach (var other in joinAck.Others)
            {
                others.Add(PlayerStateDto.CreateDtoFromState(other));
            }
        
            var packet = new JoinAckSnapshotPacket
            {
                YourCharId = joinAck.YourCharId,
                MapId = joinAck.MapId,
                Others = others
            };
            _packetSender.SendPacket(peerId, packet);
        }
        else
        {
            _logger.LogWarning("Não foi possível enviar JoinAck: CharId {CharId} não possui um peerId mapeado.", joinAck.YourCharId);
        }
    }


    /// <summary>
    /// Publica para todos os jogadores que um novo jogador entrou no mundo.
    /// </summary>
    public void Publish(in PlayerJoinedSnapshot joined)
    {
        var packet = new PlayerJoinedSnapshotPacket
        {
            NewPlayer = PlayerStateDto.CreateDtoFromState(joined.NewPlayer)
        };
        // O ideal é enviar para todos, exceto o próprio jogador que entrou.
        // A lógica para isso pode residir em um serviço de mais alto nível ou
        // o cliente pode ser programado para ignorar este pacote se o ID for o seu próprio.
        _packetSender.Broadcast(packet);
    }

    /// <summary>
    /// Publica para todos os jogadores que um jogador saiu do mundo.
    /// </summary>
    public void Publish(in PlayerLeftSnapshot left)
    {
        var packet = new PlayerLeftSnapshotPacket
        {
            LeftPlayer = PlayerStateDto.CreateDtoFromState(left.LeftPlayer)
        };
        _packetSender.Broadcast(packet);
    }

    /// <summary>
    /// Publica um snapshot de movimento para todos os jogadores.
    /// </summary>
    public void Publish(in MoveSnapshot s)
    {
        var packet = new MoveSnapshotPacket
        {
            CharId = s.CharId,
            Old = s.Old,
            New = s.New
        };
        // Em um jogo real, este envio seria otimizado para jogadores próximos (Area of Interest).
        _packetSender.Broadcast(packet, DeliveryMode.Unreliable);
    }

    /// <summary>
    /// Publica um snapshot de ataque para todos os jogadores.
    /// </summary>
    public void Publish(in AttackSnapshot s)
    {
        var packet = new AttackSnapshotPacket
        {
            CharId = s.CharId
        };
        _packetSender.Broadcast(packet);
    }

    /// <summary>
    /// Publica um snapshot de teleporte para todos os jogadores.
    /// </summary>
    public void Publish(in TeleportSnapshot s)
    {
        var packet = new TeleportSnapshotPacket
        {
            CharId = s.CharId,
            MapId = s.MapId,
            Position = s.Position
        };
        _packetSender.Broadcast(packet);
    }
    
    public void Dispose()
    {
        // Se o publisher precisar gerenciar algum recurso, a lógica de limpeza viria aqui.
        // Neste caso, não há nada para limpar.
    }
}