namespace Users.Contracts;

public record ExternalUserCreated(string Username, string Email) : IExternalEvent
{
    public string PartitionKey() => Username;
}
