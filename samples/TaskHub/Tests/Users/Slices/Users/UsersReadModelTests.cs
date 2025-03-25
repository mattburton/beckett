using TaskHub.Users.Events;
using TaskHub.Users.Slices.Users;

namespace Tests.Users.Slices.Users;

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
