using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Reservations;

public static class ReservationsEndpoint
{
    public static async Task<IResult> Handle(
        string? query,
        int? page,
        int? pageSize,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(pageParameter, pageSizeParameter);

        var result = await database.Execute(
            new ReservationsQuery(query, offset, pageSizeParameter, options),
            cancellationToken
        );

        return Results.Extensions.Render<Reservations>(
            new Reservations.ViewModel(
                result.Reservations,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
