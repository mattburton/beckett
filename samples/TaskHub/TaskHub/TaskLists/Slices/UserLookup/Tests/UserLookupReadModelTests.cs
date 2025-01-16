using TaskHub.Users.Contracts.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup.Tests;

public class UserLookupReadModelTests : StateSpecificationFixture<UserLookupReadModel>
{
    [Fact]
    public void user_added_notification()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserAddedNotification(username, email)
            )
            .Then(
                new UserLookupReadModel
                {
                    Username = username,
                    Email = email
                }
            );
    }
}
