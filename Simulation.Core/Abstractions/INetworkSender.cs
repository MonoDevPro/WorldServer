namespace Simulation.Core.Abstractions;

public interface INetworkSender
{
    void EnqueueSnapshot(int peerId, object snapshot);
}
