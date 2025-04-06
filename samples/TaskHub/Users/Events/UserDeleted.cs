namespace Users.Events;

public record UserDeleted(string Username) : IInternalEvent;
