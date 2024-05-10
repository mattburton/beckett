using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

namespace API;

public class LogSwaggerLink(
    IHostApplicationLifetime applicationLifetime,
    IServer server,
    ILogger<LogSwaggerLink> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            var address = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();

            logger.LogInformation(
                "Review the API documentation by opening your browser to {host}{path}",
                address,
                "/swagger"
            );
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
