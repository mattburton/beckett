namespace Beckett.Dashboard.Subscriptions.Actions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions/actions");

        routes.MapPost("/bulk-retry", BulkRetry.Post);
        routes.MapPost("/bulk-skip", BulkSkip.Post);
        routes.MapPost("/pause/{groupName}/{name}", Pause.Post);
        routes.MapPost("/resume/{groupName}/{name}", Resume.Post);
        routes.MapPost("/retry/{id:long}", Retry.Post);
        routes.MapPost("/skip/{id:long}", Skip.Post);

        routes.DisableAntiforgery();
    }
}
