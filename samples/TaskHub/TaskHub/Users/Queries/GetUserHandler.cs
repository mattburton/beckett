using Contracts.Users.Queries;
using TaskHub.Users.Events;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Queries;

public partial class GetUserHandler(
    IStreamReader reader
) : StreamStateQueryHandler<GetUser, GetUserHandler.State, GetUser.Result?>(reader)
{
    protected override IStreamName StreamName(GetUser query) => new UserStream(query.Username);

    protected override GetUser.Result Map(State state) => new(state.Username, state.Email);

    [State]
    public partial class State : IHaveScenarios
    {
        public string Email { get; private set; } = null!;
        public string Username { get; private set; } = null!;
        public bool Deleted { get; private set; }

        private void Apply(UserRegistered e)
        {
            Username = e.Username;
            Email = e.Email;
        }

        private void Apply(UserDeleted _) => Deleted = true;

        public IScenario[] Scenarios =>
        [
            new Scenario("user registered")
                .Given(
                    new UserRegistered(Example.String, Example.String)
                ).Then(
                    new State
                    {
                        Username = Example.String,
                        Email = Example.String,
                        Deleted = false
                    }
                ),
            new Scenario("user deleted")
                .Given(
                    new UserRegistered(Example.String, Example.String),
                    new UserDeleted(Example.String)
                ).Then(
                    new State
                    {
                        Username = Example.String,
                        Email = Example.String,
                        Deleted = true
                    }
                )
        ];
    }
}
