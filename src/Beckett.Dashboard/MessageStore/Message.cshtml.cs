namespace Beckett.Dashboard.MessageStore;

public static class MessagePage
{
    public static RouteGroupBuilder MessageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/messages/{id}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(string id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.MessageStore.GetMessage(id, cancellationToken);

        return result is null ? Results.NotFound() : new Message(new ViewModel(result));
    }

    public record ViewModel(
        GetMessageResult Message
    );
}
