using Beckett.Dashboard.Recurring.Message;
using Beckett.Database;

namespace Beckett.Dashboard.Recurring.Send;

public static class SendEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        IPostgresDatabase database,
        IMessageStore messageStore,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var recurringMessage = await database.Execute(new MessageQuery(name), cancellationToken);

        if (recurringMessage == null)
        {
            return Results.Ok();
        }

        var message = recurringMessage.ToMessage();

        await messageStore.AppendToStream(recurringMessage.StreamName, ExpectedVersion.Any, message, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("recurring_message_sent"));

        return Results.Ok();
    }
}
