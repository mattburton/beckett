using TaskHub.Users.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup;

public class UserLookupProjection(NpgsqlDataSource dataSource) : IProjection<UserLookupReadModel, string>
{
    public void Configure(IProjectionConfiguration<string> configuration)
    {
        configuration.CreatedBy<UserNotification>(x => x.Username).Where(x => x.Operation == Operation.Create);
        configuration.DeletedBy<UserNotification>(x => x.Username).Where(x => x.Operation == Operation.Delete);
    }

    public async Task Create(UserLookupReadModel readModel, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO task_lists.user_lookup (username, email) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(readModel.Username);
        command.Parameters.AddWithValue(readModel.Email);

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

    public Task Update(UserLookupReadModel readModel, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM task_lists.user_lookup WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
