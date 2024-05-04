using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

namespace MinimalApi.Infrastructure.Swagger;

internal class LogLinkToApiDocumentationAtStartup(
    IHostApplicationLifetime applicationLifetime,
    IServer server,
    ILogger<Program> logger
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
