namespace Contracts.Users.Commands;

public record RegisterUser(string Username, string Email) : ICommand;
