using Contracts.Users.Notifications;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public partial record UserLookup(string Username) : IQuery<UserLookup.State?>
{
    public class Handler(NpgsqlDataSource dataSource) : IQueryHandler<UserLookup, State?>
    {
        public async Task<State?> Handle(UserLookup query, CancellationToken cancellationToken)
        {
            const string sql = "SELECT username, email FROM task_lists.user_lookup WHERE username = $1;";

            await using var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(query.Username);

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
    }

    [State]
    public partial class State : IHaveScenarios
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        private void Apply(UserCreatedNotification message)
        {
            Username = message.Username;
            Email = message.Email;
        }

        public IScenario[] Scenarios =>
        [
            new Scenario("user created")
                .Given(new UserCreatedNotification(Example.String, Example.String))
                .Then(
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
            configuration.CreatedBy<UserCreatedNotification>(x => x.Username);
            configuration.DeletedBy<UserDeletedNotification>(x => x.Username);
        }

        public async Task Create(State state, CancellationToken cancellationToken)
        {
            const string sql = "INSERT INTO task_lists.user_lookup (username, email) VALUES ($1, $2);";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(state.Username);
            command.Parameters.AddWithValue(state.Email);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<State?> Read(string key, CancellationToken cancellationToken)
        {
            const string sql = "SELECT username, email FROM task_lists.user_lookup WHERE username = $1;";

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
            const string sql = "DELETE FROM task_lists.user_lookup WHERE username = $1;";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(key);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
