using Contracts.Users.Commands;
using TaskHub.Users.Commands;
using TaskHub.Users.Events;

namespace Tests.Users.Commands;

public class RegisterUserCommandHandlerTests : CommandHandlerFixture<RegisterUserCommand, RegisterUserCommandHandler>
{
    [Fact]
    public void user_registered()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .When(
                new RegisterUserCommand(username, email)
            )
            .Then(
                new UserRegistered(username, email)
            );
    }
}
