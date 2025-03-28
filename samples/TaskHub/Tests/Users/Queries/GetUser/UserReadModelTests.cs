using TaskHub.Users.Events;
using TaskHub.Users.Queries.GetUser;

namespace Tests.Users.Queries.GetUser;

public class UserReadModelTests : ReadModelFixture<UserReadModel>
{
    [Fact]
    public void user_registered()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserRegistered(username, email)
            ).Then(
                new UserReadModel
                {
                    Username = username,
                    Email = email,
                    Deleted = false
                }
            );
    }

    [Fact]
    public void user_deleted()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserRegistered(username, email),
                new UserDeleted(username)
            ).Then(
                new UserReadModel
                {
                    Username = username,
                    Email = email,
                    Deleted = true
                }
            );
    }
}
