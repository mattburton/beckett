using Beckett.Dashboard.MessageStore.Categories;
using Beckett.Dashboard.MessageStore.CorrelatedBy;
using Beckett.Dashboard.MessageStore.Message;
using Beckett.Dashboard.MessageStore.Messages;
using Beckett.Dashboard.MessageStore.Streams;

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

        routes.MapGet("/", CategoriesEndpoint.Handle);
        routes.MapGet("/categories/{category}", StreamsEndpoint.Handle);
        routes.MapGet("/categories/{category}/{streamName}", MessagesEndpoint.Handle);
        routes.MapGet("/correlated-by/{correlationId}", CorrelatedByEndpoint.Handle);
        routes.MapGet("/messages/{id}", MessageByIdEndpoint.Handle);
        routes.MapGet("/streams/{streamName}/{streamPosition:long}", MessageByStreamPositionEndpoint.Handle);
    }
}
