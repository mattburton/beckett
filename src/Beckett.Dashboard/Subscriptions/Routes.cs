using Beckett.Dashboard.Subscriptions.GetSubscription;
using Beckett.Dashboard.Subscriptions.GetSubscriptions;
using Beckett.Dashboard.Subscriptions.Pause;
using Beckett.Dashboard.Subscriptions.Resume;

namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", GetSubscriptionsHandler.Get);
        routes.MapGet("/{groupName}/{name}", GetSubscriptionHandler.Get);
        routes.MapPost("/{groupName}/{name}/pause", PauseHandler.Post).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/resume", ResumeHandler.Post).DisableAntiforgery();
    }
}
