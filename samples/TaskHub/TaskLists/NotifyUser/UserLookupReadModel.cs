using Users.Contracts;

namespace TaskLists.NotifyUser;

[State]
public partial class UserLookupReadModel : IHaveScenarios
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(ExternalUserCreated message)
    {
        Username = message.Username;
        Email = message.Email;
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user created")
            .Given(new ExternalUserCreated(Example.String, Example.String))
            .Then(
                new UserLookupReadModel
                {
                    Username = Example.String,
                    Email = Example.String
                }
            )
    ];
}
