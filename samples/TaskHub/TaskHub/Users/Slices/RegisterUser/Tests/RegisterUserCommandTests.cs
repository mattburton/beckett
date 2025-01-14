using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.RegisterUser.Tests;

public class RegisterUserCommandTests : CommandSpecificationFixture<RegisterUserCommand>
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
