namespace Beckett.Dashboard.MessageStore;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        if (!Dashboard.Options.MessageStoreEnabled)
        {
            return;
        }

        var routes = builder.MapGroup("/message-store");

        routes.MapGet("/", Index.Get);
        routes.MapGet("/categories/{category}", Streams.Get);
        routes.MapGet("/categories/{category}/{streamName}", Messages.Get);
        routes.MapGet("/correlated-by/{correlationId}", CorrelatedBy.Get);
        routes.MapGet("/messages/{id}", Message.GetById);
        routes.MapGet("/streams/{streamName}/{streamPosition:long}", Message.GetByStreamPosition);
    }
}
