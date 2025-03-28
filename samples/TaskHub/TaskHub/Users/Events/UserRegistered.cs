namespace TaskHub.Users.Events;

public record UserRegistered(string Username, string Email) : IEvent;
