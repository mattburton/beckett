namespace Beckett.Dashboard.MessageStore.Message;

public static class MessageByIdEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var result = await dashboard.MessageStore.GetMessage(id, cancellationToken);

        return result is null ? Results.NotFound() : Results.Extensions.Render<Message>(new Message.ViewModel(result));
    }
}
