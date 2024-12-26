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

        routes.MapGet("/", CategoriesHandler.Get);
        routes.MapGet("/categories/{category}", StreamsHandler.Get);
        routes.MapGet("/categories/{category}/{streamName}", MessagesHandler.Get);
        routes.MapGet("/correlated-by/{correlationId}", CorrelatedByHandler.Get);
        routes.MapGet("/messages/{id}", MessageHandler.GetById);
        routes.MapGet("/streams/{streamName}/{streamPosition:long}", MessageHandler.GetByStreamPosition);
    }
}
