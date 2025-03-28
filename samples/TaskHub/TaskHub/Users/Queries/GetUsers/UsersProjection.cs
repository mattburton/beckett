using TaskHub.Users.Events;

namespace TaskHub.Users.Queries.GetUsers;

public class UsersProjection(NpgsqlDataSource dataSource) : IProjection<UsersReadModel, string>
{
    public void Configure(IProjectionConfiguration<string> configuration)
    {
        configuration.CreatedBy<UserRegistered>(x => x.Username);
        configuration.DeletedBy<UserDeleted>(x => x.Username);
    }

    public async Task Create(UsersReadModel readModel, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO users.users (username, email) VALUES ($1, $2);";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(readModel.Username);
        command.Parameters.AddWithValue(readModel.Email);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UsersReadModel?> Read(string key, CancellationToken cancellationToken)
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

        return new UsersReadModel
        {
            Username = reader.GetFieldValue<string>(0),
            Email = reader.GetFieldValue<string>(1)
        };
    }

    public Task Update(UsersReadModel readModel, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM users.users WHERE username = $1;";

        var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
