namespace Contracts.Users.Commands;

public record DeleteUser(string Username) : ICommand;
