namespace Users.Events;

public record UserRegistered(string Username, string Email) : IInternalEvent;
