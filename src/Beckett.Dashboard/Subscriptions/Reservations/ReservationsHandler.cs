namespace Beckett.Dashboard.Subscriptions.Reservations;

public static class ReservationsHandler
{
    public static async Task<IResult> Get(
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.Subscriptions.GetReservations(
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Reservations>(
            new Reservations.ViewModel(result.Reservations, query, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }
}
