using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUsers;

public class GetUsersReadModelProjection(NpgsqlDataSource dataSource) : IProjection<GetUsersReadModel, string>
{
    public void Configure(IProjectionConfiguration<string> configuration)
    {
        configuration.CreatedBy<UserRegistered>(x => x.Username);
        configuration.DeletedBy<UserDeleted>(x => x.Username);
    }

    public async Task Create(GetUsersReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO users.get_users_read_model (username, email) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Username);
        command.Parameters.AddWithValue(state.Email);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GetUsersReadModel?> Read(string key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT username, email FROM users.get_users_read_model WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new GetUsersReadModel
        {
            Username = reader.GetFieldValue<string>(0),
            Email = reader.GetFieldValue<string>(1)
        };
    }

    public Task Update(GetUsersReadModel state, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task Delete(GetUsersReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM users.get_users_read_model WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Username);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
