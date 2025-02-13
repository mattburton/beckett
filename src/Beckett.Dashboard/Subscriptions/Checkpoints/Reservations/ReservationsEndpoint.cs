namespace Beckett.Dashboard.Subscriptions.Checkpoints.Reservations;

public static class ReservationsEndpoint
{
    public static async Task<IResult> Handle(
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
