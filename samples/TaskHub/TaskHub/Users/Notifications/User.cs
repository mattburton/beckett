namespace TaskHub.Users.Notifications;

public record User(Operation Operation, string Username, string Email) : INotification;
