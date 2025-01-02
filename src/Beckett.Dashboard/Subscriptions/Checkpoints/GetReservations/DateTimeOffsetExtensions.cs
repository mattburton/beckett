namespace Beckett.Dashboard.Subscriptions.Checkpoints.GetReservations;

public static class DateTimeOffsetExtensions
{
    public static string ToFriendlyTimeAbbreviation(this DateTimeOffset timestamp)
    {
        var now = DateTimeOffset.UtcNow;

        var difference = timestamp - now;

        string result;

        if (difference.Days > 0)
        {
            result = $"{difference.Days} day{(difference.Days > 1 ? "s" : "")}";
        }
        else if (difference.Hours > 0)
        {
            result = $"{difference.Hours} hr{(difference.Hours > 1 ? "s" : "")}";
        }
        else if (difference.Minutes > 0)
        {
            result = $"{difference.Minutes} min{(difference.Minutes > 1 ? "s" : "")}";
        }
        else
        {
            result = difference.Seconds > 0
                ? $"{difference.Seconds}s"
                : $"{difference.Milliseconds}ms";
        }

        if (timestamp < now)
        {
            result += " ago";
        }

        return result;
    }
}
