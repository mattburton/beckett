using Contracts.TaskLists.Queries;
using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Queries;

public partial class GetTaskListsHandler(NpgsqlDataSource dataSource) : IQueryHandler<GetTaskLists, GetTaskLists.Result>
{
    public async Task<GetTaskLists.Result> Handle(
        GetTaskLists query,
        CancellationToken cancellationToken
    )
    {
        const string sql = "SELECT id, name FROM task_lists.task_lists;";

        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetTaskLists.TaskList>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetTaskLists.TaskList(reader.GetFieldValue<Guid>(0), reader.GetFieldValue<string>(1))
            );
        }

        return new GetTaskLists.Result(results);
    }

    [State]
    public partial class State : IHaveScenarios
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        private void Apply(TaskListAdded message)
        {
            Id = message.Id;
            Name = message.Name;
        }

        private void Apply(TaskListNameChanged message) => Name = message.Name;

        public IScenario[] Scenarios =>
        [
            new Scenario("task list added")
                .Given(new TaskListAdded(Example.Guid, Example.String))
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = Example.String
                    }
                ),
            new Scenario("task list name changed")
                .Given(
                    new TaskListAdded(Example.Guid, "old name"),
                    new TaskListNameChanged(Example.Guid, "new name")
                )
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = "new name"
                    }
                )
        ];
    }

    public class Projection(NpgsqlDataSource dataSource) : IProjection<State, Guid>
    {
        public void Configure(IProjectionConfiguration<Guid> configuration)
        {
            configuration.CreatedBy<TaskListAdded>(x => x.Id);
            configuration.UpdatedBy<TaskListNameChanged>(x => x.Id);
            configuration.DeletedBy<TaskListDeleted>(x => x.Id);
        }

        public async Task Create(State state, CancellationToken cancellationToken)
        {
            const string sql = "INSERT INTO task_lists.task_lists (id, name) VALUES ($1, $2);";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(state.Id);
            command.Parameters.AddWithValue(state.Name);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<State?> Read(Guid key, CancellationToken cancellationToken)
        {
            const string sql = "SELECT id, name FROM task_lists.task_lists WHERE id = $1;";

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
                Id = reader.GetFieldValue<Guid>(0),
                Name = reader.GetFieldValue<string>(1)
            };
        }

        public async Task Update(State state, CancellationToken cancellationToken)
        {
            const string sql = "UPDATE task_lists.task_lists SET name = $2 WHERE id = $1;";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(state.Id);
            command.Parameters.AddWithValue(state.Name);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task Delete(Guid key, CancellationToken cancellationToken)
        {
            const string sql = "DELETE FROM task_lists.task_lists WHERE id = $1;";

            var command = dataSource.CreateCommand(sql);

            command.Parameters.AddWithValue(key);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }


}
