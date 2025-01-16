using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.Users.Tests;

public class UsersReadModelTests : StateSpecificationFixture<UsersReadModel>
{
    [Fact]
    public void user_registered()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserRegistered(username, email)
            )
            .Then(
                new UsersReadModel
                {
                    Username = username,
                    Email = email
                }
            );
    }
}
