using TaskHub.Users.Contracts.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup;

public class UserLookupProjection(NpgsqlDataSource dataSource) : IProjection<UserLookupReadModel, string>
{
    public void Configure(IProjectionConfiguration<string> configuration)
    {
        configuration.CreatedBy<UserAddedNotification>(x => x.Username);
    }

    public async Task Create(UserLookupReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO task_lists.user_lookup_read_model (username, email) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Username);
        command.Parameters.AddWithValue(state.Email);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UserLookupReadModel?> Read(string key, CancellationToken cancellationToken)
    {
        const string sql = "SELECT username, email FROM task_lists.user_lookup_read_model WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new UserLookupReadModel
        {
            Username = reader.GetFieldValue<string>(0),
            Email = reader.GetFieldValue<string>(1)
        };
    }

    public Task Update(UserLookupReadModel state, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task Delete(string key, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
