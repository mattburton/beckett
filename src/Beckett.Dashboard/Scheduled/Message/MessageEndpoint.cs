using Beckett.Database;

namespace Beckett.Dashboard.Scheduled.Message;

public static class MessageEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        Guid id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var viewModel = await database.Execute(new Query(id, options), cancellationToken);

        if (viewModel is not null)
        {
            return Results.Extensions.Render<Message>(viewModel);
        }

        if (!context.Request.Headers.ContainsKey("HX-Request"))
        {
            return Results.Redirect($"{Dashboard.Prefix}/scheduled");
        }

        context.Response.Headers.Append("HX-Redirect", new StringValues($"{Dashboard.Prefix}/scheduled"));

        return Results.Ok();

    }
}
