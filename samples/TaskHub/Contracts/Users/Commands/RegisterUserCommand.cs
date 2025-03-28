namespace Contracts.Users.Commands;

public record RegisterUserCommand(string Username, string Email) : ICommand;
