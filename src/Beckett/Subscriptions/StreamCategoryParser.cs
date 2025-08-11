namespace Beckett.Subscriptions;

public static class StreamCategoryParser
{
    public static string Parse(string streamName)
    {
        return !streamName.Contains('-') ? streamName : streamName[..streamName.IndexOf('-')];
    }
}
