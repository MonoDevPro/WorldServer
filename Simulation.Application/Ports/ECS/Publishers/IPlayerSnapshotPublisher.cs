using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Snapshots;

namespace Simulation.Application.Ports.ECS.Publishers;

public interface IPlayerSnapshotPublisher : IDisposable
{
    // envia mensagem direta para o cliente que entrou — dto deve conter SessionId ou ser acompanhado dele
    void Publish(JoinAckDto joinAck);

    // broadcast para todos no mapa sobre novo jogador
    void Publish(PlayerJoinedDto joined);

    // broadcast para todos no mapa sobre jogador saindo
    void Publish(PlayerLeftDto left);

    // hot-path: movimentação / combate / teleporte (passar por 'in' se structs)
    void Publish(in MoveSnapshot s);
    void Publish(in AttackSnapshot s);
    void Publish(in TeleportSnapshot s);
    
}