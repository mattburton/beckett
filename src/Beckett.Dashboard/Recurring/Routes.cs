using Beckett.Dashboard.Recurring.Message;
using Beckett.Dashboard.Recurring.Messages;
using Beckett.Dashboard.Recurring.Send;

namespace Beckett.Dashboard.Recurring;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/recurring");

        routes.MapGet("/", MessagesEndpoint.Handle);
        routes.MapGet("/{name}", MessageEndpoint.Handle);
        routes.MapPost("/{name}/send", SendEndpoint.Handle);
    }
}
