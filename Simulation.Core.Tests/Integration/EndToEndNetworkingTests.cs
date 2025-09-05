using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Server;
using Xunit;
// Add the namespace where AddServerServices is defined

namespace Simulation.Core.Tests.Integration;

public class EndToEndNetworkingTests
{
    [Fact(Skip = "Smoke test placeholder: requires real network setup")] 
    public async Task ServerStartsAndRunsMainLoop()
    {
        var services = new ServiceCollection();
        services.AddServerServices();
        var provider = services.BuildServiceProvider();
        var server = provider.GetRequiredService<ServerLoop>();

        using var cts = new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<TaskCanceledException>(async () =>
        {
            await server.RunAsync(cts.Token);
        });
    }
}
