namespace Contracts.Users.Notifications;

public record UserDeletedNotification(string Username) : INotification
{
    public string PartitionKey() => Username;
}
