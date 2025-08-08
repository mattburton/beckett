using Beckett.Database;

namespace Beckett.Dashboard.Recurring.Messages;

public static class MessagesEndpoint
{
    public static async Task<IResult> Handle(
        IPostgresDatabase database,
        string? query = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await database.Execute(new MessagesQuery(query, page, pageSize), cancellationToken);

        var model = new Messages.ViewModel(
            result.RecurringMessages,
            query,
            page.ToPageParameter(),
            pageSize.ToPageSizeParameter(),
            result.TotalResults
        );

        return Results.Extensions.Render<Messages>(model);
    }
}
