using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUser.Tests;

public class GetUserReadModelTests : StateSpecificationFixture<GetUserReadModel>
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
                new GetUserReadModel
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
                new GetUserReadModel
                {
                    Username = username,
                    Email = email,
                    Deleted = true
                }
            );
    }
}
