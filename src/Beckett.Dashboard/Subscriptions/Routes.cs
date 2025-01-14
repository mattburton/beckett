using Beckett.Dashboard.Subscriptions.GetSubscription;
using Beckett.Dashboard.Subscriptions.GetSubscriptions;
using Beckett.Dashboard.Subscriptions.Pause;
using Beckett.Dashboard.Subscriptions.Reset;
using Beckett.Dashboard.Subscriptions.Resume;

namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", GetSubscriptionsEndpoint.Handle);
        routes.MapGet("/{groupName}/{name}", GetSubscriptionEndpoint.Handle);
        routes.MapPost("/{groupName}/{name}/pause", PauseEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/reset", ResetEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/resume", ResumeEndpoint.Handle).DisableAntiforgery();
    }
}
