using Users.Contracts;

namespace TaskLists.NotifyUser;

public class UserLookupProjection(NpgsqlDataSource dataSource) : IProjection<UserLookupReadModel, string>
{
    public void Configure(IProjectionConfiguration<string> configuration)
    {
        configuration.CreatedBy<ExternalUserCreated>(x => x.Username);
        configuration.DeletedBy<ExternalUserDeleted>(x => x.Username);
    }

    public async Task Create(UserLookupReadModel state, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO task_lists.user_lookup (username, email) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(state.Username);
        command.Parameters.AddWithValue(state.Email);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UserLookupReadModel?> Read(string key, CancellationToken cancellationToken)
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

        return new UserLookupReadModel
        {
            Username = reader.GetFieldValue<string>(0),
            Email = reader.GetFieldValue<string>(1)
        };
    }

    public Task Update(UserLookupReadModel state, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM task_lists.user_lookup WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
