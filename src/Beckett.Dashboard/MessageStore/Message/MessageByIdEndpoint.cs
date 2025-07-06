using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.Message;

public static class MessageByIdEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new InvalidOperationException("Invalid message ID");
        }

        var result = await database.Execute(new MessageQuery(guid), cancellationToken);

        return result is null ? Results.NotFound() : Results.Extensions.Render<Message>(new Message.ViewModel(result));
    }
}
