using Microsoft.Extensions.Configuration;
using Simulation.Network;

namespace Simulation.Client;

class Program
{
    static void Main()
    {
        // --- Carrega a configuração ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var networkOptions = new NetworkOptions();
        configuration.GetSection(NetworkOptions.SectionName).Bind(networkOptions);
        // --- Fim da seção de configuração ---
    }
}