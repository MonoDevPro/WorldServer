using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using LiteNetLib;
using LiteNetLib.Utils;
using Simulation.Domain;
using Simulation.ECS.Events;

namespace Simulation.ECS.Systems;

public sealed partial class NetworkSnapshotSystem(World world, NetManager manager)
    : BaseSystem<World, float>(world)
{
    private float _timeSinceLastSnapshot = 0f;
    private const float SnapshotInterval = 1f / 20f; // 20 snapshots por segundo
    private readonly NetDataWriter _writer = new();
    
    // Este objeto irá conter APENAS os estados que mudaram neste tick.
    private readonly WorldStateSnapshot _worldStateSnapshot = new();
    
    // Este dicionário armazena o último estado que foi ENVIADO para cada jogador.
    private readonly Dictionary<int, PlayerStateSnapshot> _lastSentStates = new();

    // NOTA: A query foi movida para DENTRO do método Update para ter acesso
    // à lista _worldStateSnapshot, que agora é o nosso buffer de coleta.
    [Query]
    [All<Position, Direction, Health>]
    private void CollectChangedPlayerSnapshots(in CharId charId, in Position pos, in Direction dir, in Health health)
    {
        var snapshot = new PlayerStateSnapshot
        {
            CharId = charId.Value,
            PosX = pos.X,
            PosY = pos.Y,
            DirX = dir.X,
            DirY = dir.Y,
            CurrentHealth = health.Current
        };

        // Verifica se o estado mudou desde o último envio
        if (_lastSentStates.TryGetValue(charId.Value, out var lastState) && 
            lastState.PosX == snapshot.PosX &&
            lastState.PosY == snapshot.PosY &&
            lastState.DirX == snapshot.DirX &&
            lastState.DirY == snapshot.DirY &&
            lastState.CurrentHealth == snapshot.CurrentHealth)
        {
            // Estado não mudou, ignora.
            return;
        }
        
        // CORREÇÃO: O estado mudou, então ADICIONA o snapshot ao pacote a ser enviado.
        _worldStateSnapshot.PlayerStates.Add(snapshot);
        
        // E atualiza o último estado conhecido para a próxima comparação.
        _lastSentStates[charId.Value] = snapshot;
    }
    
    public override void Update(in float deltaTime)
    {
        _timeSinceLastSnapshot += deltaTime;
        if (_timeSinceLastSnapshot < SnapshotInterval)
            return;
        
        _timeSinceLastSnapshot = 0f;
        
        // 1. Limpa a lista do snapshot a ser enviado neste tick.
        _worldStateSnapshot.PlayerStates.Clear();
        
        // 2. CORREÇÃO: Executa a query para popular a lista _worldStateSnapshot
        //    com os estados que mudaram. O nome do método gerado é o nome do
        //    seu método com "Query" no final.
        CollectChangedPlayerSnapshotsQuery(World);
        
        // 3. CORREÇÃO: Verifica se HÁ snapshots para enviar.
        if (_worldStateSnapshot.PlayerStates.Count > 0)
        {
            // 4. CORREÇÃO: Serializa o pacote com os estados alterados.
            _writer.Reset();
            _worldStateSnapshot.Serialize(_writer);
            
            // 5. Envia os dados para todos os clientes.
            manager.SendToAll(_writer, DeliveryMethod.Unreliable);
        }
    }
}