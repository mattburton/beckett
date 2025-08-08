using Beckett.Database;

namespace Beckett.Dashboard.Recurring.Message;

public static class MessageEndpoint
{
    public static async Task<IResult> Handle(
        IPostgresDatabase database,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var recurringMessage = await database.Execute(new MessageQuery(name), cancellationToken);

        if (recurringMessage == null)
        {
            return Results.Extensions.Render<MessageNotFound>();
        }

        var model = new Message.ViewModel(recurringMessage);

        return Results.Extensions.Render<Message>(model);
    }
}
