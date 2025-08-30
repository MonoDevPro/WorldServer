using LiteNetLib;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Network;

public class NetworkHandler(INetEventListener listener): IIntentHandler
{
    public void EnqueueEnterGameIntent(EnterGameIntent intent, in CharTemplate template)
    {
        throw new NotImplementedException();
    }

    public void EnqueueExitGameIntent(ExitGameIntent intent)
    {
        throw new NotImplementedException();
    }

    public void EnqueueMoveIntent(MoveIntent intent)
    {
        throw new NotImplementedException();
    }

    public void EnqueueTeleportIntent(TeleportIntent intent)
    {
        throw new NotImplementedException();
    }

    public void EnqueueAttackIntent(AttackIntent intent)
    {
        throw new NotImplementedException();
    }
}