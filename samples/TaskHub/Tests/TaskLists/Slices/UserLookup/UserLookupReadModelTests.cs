using TaskHub.TaskLists.Slices.UserLookup;
using TaskHub.Users.Notifications;

namespace Tests.TaskLists.Slices.UserLookup;

public class UserLookupReadModelTests : ReadModelFixture<UserLookupReadModel>
{
    [Fact]
    public void user_notification()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserNotification(Operation.Create, username, email)
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
