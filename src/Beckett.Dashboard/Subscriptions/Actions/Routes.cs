using Beckett.Dashboard.Subscriptions.Actions.Handlers;

namespace Beckett.Dashboard.Subscriptions.Actions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions/actions");

        routes.MapPost("/bulk-retry", BulkRetryHandler.Post);
        routes.MapPost("/bulk-skip", BulkSkipHandler.Post);
        routes.MapPost("/pause/{groupName}/{name}", PauseHandler.Post);
        routes.MapPost("/resume/{groupName}/{name}", ResumeHandler.Post);
        routes.MapPost("/retry/{id:long}", RetryHandler.Post);
        routes.MapPost("/skip/{id:long}", SkipHandler.Post);

        routes.DisableAntiforgery();
    }
}
