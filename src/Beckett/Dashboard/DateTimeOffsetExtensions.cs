namespace Beckett.Dashboard;

public static class DateTimeOffsetExtensions
{
    public static string ToFriendlyTimeDisplay(this DateTimeOffset timestamp)
    {
        var now = DateTimeOffset.UtcNow;

        if (timestamp < now)
        {
            return "Waiting";
        }

        var difference = timestamp - now;

        if (difference.Days > 0)
        {
            return $"{difference.Days} day{(difference.Days > 1 ? "s" : "")}";
        }

        if (difference.Hours > 0)
        {
            return $"{difference.Hours} hour{(difference.Hours > 1 ? "s" : "")}";
        }

        if (difference.Minutes > 0)
        {
            return $"{difference.Minutes} minute{(difference.Minutes > 1 ? "s" : "")}";
        }

        return difference.Seconds > 0
            ? $"{difference.Seconds} second{(difference.Seconds > 1 ? "s" : "")}"
            : $"{difference.Milliseconds} millisecond{(difference.Milliseconds > 1 ? "s" : "")}";
    }

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
