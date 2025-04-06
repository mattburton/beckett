namespace Users;

public record UserStream(string Username) : IStreamName
{
    public const string Category = "User";

    public string StreamName() => $"{Category}-{Username}";
}
