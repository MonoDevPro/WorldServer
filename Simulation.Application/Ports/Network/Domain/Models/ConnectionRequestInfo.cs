namespace Simulation.Application.Ports.Network.Domain.Models;

    /// <summary>
    /// Informações de uma requisição de conexão
    /// </summary>
    public class ConnectionRequestInfo
    {
        public string RemoteEndPoint { get; }
        public string ConnectionKey { get; }
        
        public ConnectionRequestInfo(string remoteEndPoint, string connectionKey)
        {
            RemoteEndPoint = remoteEndPoint;
            ConnectionKey = connectionKey;
        }
    }