namespace Contracts.Users.Commands;

public record DeleteUserCommand(string Username) : ICommand;
