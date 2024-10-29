namespace Beckett.Dashboard.Subscriptions;

public record GetReservationsResult(List<GetReservationsResult.Reservation> Reservations, int TotalResults)
{
    public record Reservation(
        long Id,
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition,
        DateTimeOffset ReservedUntil
    );
}
