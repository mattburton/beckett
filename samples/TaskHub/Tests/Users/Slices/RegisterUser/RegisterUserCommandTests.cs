using TaskHub.Users.Events;
using TaskHub.Users.Slices.RegisterUser;

namespace Tests.Users.Slices.RegisterUser;

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
