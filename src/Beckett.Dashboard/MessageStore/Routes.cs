using Beckett.Dashboard.MessageStore.GetCategories;
using Beckett.Dashboard.MessageStore.GetCorrelatedBy;
using Beckett.Dashboard.MessageStore.GetMessage;
using Beckett.Dashboard.MessageStore.GetMessages;
using Beckett.Dashboard.MessageStore.GetStreams;

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

        routes.MapGet("/", GetCategoriesHandler.Get);
        routes.MapGet("/categories/{category}", GetStreamsHandler.Get);
        routes.MapGet("/categories/{category}/{streamName}", GetMessagesHandler.Get);
        routes.MapGet("/correlated-by/{correlationId}", GetCorrelatedByHandler.Get);
        routes.MapGet("/messages/{id}", GetMessageHandler.GetById);
        routes.MapGet("/streams/{streamName}/{streamPosition:long}", GetMessageHandler.GetByStreamPosition);
    }
}
