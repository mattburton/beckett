namespace TaskHub.Users.Notifications;

public record UserNotification(Operation Operation, string Username, string Email) : INotification;
