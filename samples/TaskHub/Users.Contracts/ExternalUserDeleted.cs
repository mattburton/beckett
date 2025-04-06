namespace Users.Contracts;

public record ExternalUserDeleted(string Username) : IExternalEvent
{
    public string PartitionKey() => Username;
}
