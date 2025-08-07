using Beckett.Dashboard.Subscriptions.Groups;
using Beckett.Dashboard.Subscriptions.Pause;
using Beckett.Dashboard.Subscriptions.Replay;
using Beckett.Dashboard.Subscriptions.Reset;
using Beckett.Dashboard.Subscriptions.Resume;
using Beckett.Dashboard.Subscriptions.Subscription;
using Beckett.Dashboard.Subscriptions.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", GroupsEndpoint.Handle);
        routes.MapGet("/{groupName}", SubscriptionsEndpoint.Handle);
        routes.MapGet("/{groupName}/{name}", SubscriptionEndpoint.Handle);
        routes.MapPost("/{groupName}/{name}/pause", PauseEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/replay", ReplayEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/reset", ResetEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{groupName}/{name}/resume", ResumeEndpoint.Handle).DisableAntiforgery();
    }
}
