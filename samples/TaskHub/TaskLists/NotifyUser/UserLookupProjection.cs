using NpgsqlTypes;
using Users.Contracts;

namespace TaskLists.NotifyUser;

public class UserLookupProjection(NpgsqlDataSource dataSource) : PostgresProjection<UserLookupReadModel>(dataSource)
{
    public override void Configure(IProjectionConfiguration configuration)
    {
        configuration.CreatedBy<ExternalUserCreated>(x => x.Username);
        configuration.DeletedBy<ExternalUserDeleted>(x => x.Username);
    }

    public override object GetKey(UserLookupReadModel state) => state.Username;

    protected override async Task<IReadOnlyList<UserLookupReadModel>> Load(
        IReadOnlyList<object> keys,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT username, email FROM task_lists.user_lookup WHERE username = ANY($1);";

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(NpgsqlDbType.Text | NpgsqlDbType.Array, keys);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<UserLookupReadModel>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new UserLookupReadModel
                {
                    Username = reader.GetFieldValue<string>(0),
                    Email = reader.GetFieldValue<string>(1)
                }
            );
        }

        return results;
    }

    protected override NpgsqlBatchCommand SaveCommand(UserLookupReadModel state)
    {
        const string sql = """
            INSERT INTO task_lists.user_lookup (username, email)
            VALUES ($1, $2)
            ON CONFLICT (username) DO UPDATE SET email = $2;
        """;

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Username);
        command.Parameters.AddWithValue(state.Email);

        return command;
    }

    protected override NpgsqlBatchCommand DeleteCommand(UserLookupReadModel state)
    {
        const string sql = "DELETE FROM task_lists.user_lookup WHERE username = $1;";

        var command = new NpgsqlBatchCommand(sql);

        command.Parameters.AddWithValue(state.Username);

        return command;
    }
}
