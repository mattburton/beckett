using Beckett.Dashboard.Scheduled.Cancel;
using Beckett.Dashboard.Scheduled.Message;
using Beckett.Dashboard.Scheduled.Messages;

namespace Beckett.Dashboard.Scheduled;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/scheduled");

        routes.MapGet("/", MessagesEndpoint.Handle);
        routes.MapGet("/{id:guid}", MessageEndpoint.Handle);
        routes.MapPost("/{id:guid}/cancel", CancelEndpoint.Handle);
    }
}
