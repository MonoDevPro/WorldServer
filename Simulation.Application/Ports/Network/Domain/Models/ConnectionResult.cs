namespace Simulation.Application.Ports.Network.Domain.Models;

    /// <summary>
    /// Resultado de uma tentativa de conexão
    /// </summary>
    public class ConnectionResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        
        public ConnectionResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }