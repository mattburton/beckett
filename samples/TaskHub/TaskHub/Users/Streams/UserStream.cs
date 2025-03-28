using Core.Streams;

namespace TaskHub.Users.Streams;

public record UserStream(string Username) : IStreamName
{
    public const string Category = "User";

    public string StreamName() => $"{Category}-{Username}";
}
