using Simulation.Application.DTOs;

namespace Simulation.Application.Ports.Char;

public interface ICharSnapshotPublisher : IDisposable
{
    public void Publish(in EnterSnapshot snapshot);
    public void Publish(in CharSnapshot snapshot);
    public void Publish(in ExitSnapshot snapshot);
    public void Publish(in MoveSnapshot snapshot);
    public void Publish(in AttackSnapshot snapshot);
    public void Publish(in TeleportSnapshot snapshot);
}