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

        routes.MapGet("/", GetCategoriesEndpoint.Handle);
        routes.MapGet("/categories/{category}", GetStreamsEndpoint.Handle);
        routes.MapGet("/categories/{category}/{streamName}", GetMessagesEndpoint.Handle);
        routes.MapGet("/correlated-by/{correlationId}", GetCorrelatedByEndpoint.Handle);
        routes.MapGet("/messages/{id}", GetMessageByIdEndpoint.Handle);
        routes.MapGet("/streams/{streamName}/{streamPosition:long}", GetMessageByStreamPositionEndpoint.Handle);
    }
}
