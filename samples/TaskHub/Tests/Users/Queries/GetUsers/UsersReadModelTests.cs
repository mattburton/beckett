using TaskHub.Users.Events;
using TaskHub.Users.Queries.GetUsers;

namespace Tests.Users.Queries.GetUsers;

public class UsersReadModelTests : ReadModelFixture<UsersReadModel>
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
