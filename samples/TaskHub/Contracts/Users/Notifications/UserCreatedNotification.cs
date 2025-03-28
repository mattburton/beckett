namespace Contracts.Users.Notifications;

public record UserCreatedNotification(string Username, string Email) : INotification
{
    public string PartitionKey() => Username;
}
