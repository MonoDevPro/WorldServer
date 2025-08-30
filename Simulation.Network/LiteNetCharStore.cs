using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace Simulation.Network;

public class LiteNetPeerStore
{
    private record PeerInfo(int CharId, NetPeer Peer);
    
    private readonly Dictionary<int, PeerInfo> _peersByCharId = new();
    private readonly Dictionary<NetPeer, int> _charIdByPeer = new();
    
    public bool TryAuthenticate(int charId, string name, NetPeer peer)
    {
        if (_peersByCharId.ContainsKey(charId) || _charIdByPeer.ContainsKey(peer))
            return false;
        
        var info = new PeerInfo(charId, peer);
        _peersByCharId[charId] = info;
        _charIdByPeer[peer] = charId;
        return true;
    }
    
    public bool TryGetPeerByCharId(int charId, out NetPeer? peer)
    {
        if (_peersByCharId.TryGetValue(charId, out var info))
        {
            peer = info.Peer;
            return true;
        }

        peer = null;
        return false;
    }
}