namespace Beckett.Dashboard.Subscriptions;

public static class ReservationsPage
{
    public static RouteGroupBuilder ReservationsPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/reservations", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
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

        return new Reservations(
            new ViewModel(result.Reservations, query, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }

    public record ViewModel(
        List<GetReservationsResult.Reservation> Reservations,
        string? Query,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
