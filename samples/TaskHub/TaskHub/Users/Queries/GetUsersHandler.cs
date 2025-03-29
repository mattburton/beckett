using Contracts.Users.Queries;
using TaskHub.Users.Events;

namespace TaskHub.Users.Queries;

public partial class GetUsersHandler(NpgsqlDataSource dataSource) : IQueryHandler<GetUsers, GetUsers.Result>
{
    public async Task<GetUsers.Result> Handle(GetUsers query, CancellationToken cancellationToken)
    {
        const string sql = "SELECT username, email FROM users.users;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetUsers.User>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetUsers.User(reader.GetFieldValue<string>(0), reader.GetFieldValue<string>(1))
            );
        }

        return new GetUsers.Result(results);
    }

    [State]
    public partial class State : IHaveScenarios
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        private void Apply(UserRegistered message)
        {
            Username = message.Username;
            Email = message.Email;
        }

        public IScenario[] Scenarios =>
        [
            new Scenario("user registered")
                .Given(
                    new UserRegistered(Example.String, Example.String)
                ).Then(
                    new State
                    {
                        Username = Example.String,
                        Email = Example.String
                    }
                )
        ];
    }

    public class Projection(NpgsqlDataSource dataSource) : IProjection<State, string>
    {
        public void Configure(IProjectionConfiguration<string> configuration)
        {
            configuration.CreatedBy<UserRegistered>(x => x.Username);
            configuration.DeletedBy<UserDeleted>(x => x.Username);
        }

        public async Task Create(State state, CancellationToken cancellationToken)
        {
            const string sql = "INSERT INTO users.users (username, email) VALUES ($1, $2);";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(state.Username);
            command.Parameters.AddWithValue(state.Email);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<State?> Read(string key, CancellationToken cancellationToken)
        {
            const string sql = "SELECT username, email FROM users.users WHERE username = $1;";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(key);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            await reader.ReadAsync(cancellationToken);

            if (!reader.HasRows)
            {
                return null;
            }

            return new State
            {
                Username = reader.GetFieldValue<string>(0),
                Email = reader.GetFieldValue<string>(1)
            };
        }

        public Task Update(State state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public async Task Delete(string key, CancellationToken cancellationToken)
        {
            const string sql = "DELETE FROM users.users WHERE username = $1;";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(key);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
