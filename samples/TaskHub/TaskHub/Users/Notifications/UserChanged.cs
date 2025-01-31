namespace TaskHub.Users.Notifications;

public record UserChanged(string Username, string Email) : INotification;
