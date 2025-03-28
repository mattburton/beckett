using Contracts.TaskLists.Queries;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Queries.GetTaskLists;

namespace Tests.TaskLists.Queries.GetTaskLists;

public class GetTaskListsIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task reads_read_model()
    {
        var state = GenerateReadModel();
        var projection = new TaskListsProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);

        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new TaskListsProjection(database.DataSource);

        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task updates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new TaskListsProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        state.Name = "updated name";
        await projection.Update(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task deletes_read_model()
    {
        var state = GenerateReadModel();
        var projection = new TaskListsProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Id, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_all_rows()
    {
        var data = new List<TaskListsReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var expectedResult = new GetTaskListsQuery.Result(
            data.Select(x => new GetTaskListsQuery.TaskList(x.Id, x.Name)).ToList()
        );
        var handler = new GetTaskListsQueryHandler(database.DataSource);
        var query = new GetTaskListsQuery();
        await SetupDatabase(data);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        foreach (var expectedTaskList in expectedResult.TaskLists)
        {
            Assert.Contains(result.TaskLists, actualTaskList => actualTaskList.Id == expectedTaskList.Id);
        }
    }

    private async Task SetupDatabase(List<TaskListsReadModel> data)
    {
        var projection = new TaskListsProjection(database.DataSource);

        foreach (var row in data)
        {
            await projection.Create(row, CancellationToken.None);
        }
    }

    private static TaskListsReadModel GenerateReadModel() => ReadModelBuilder.Build<TaskListsReadModel>(
        new TaskListAdded(Generate.Guid(), Generate.String())
    );
}
