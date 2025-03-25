using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.TaskLists;

namespace Tests.TaskLists.Slices.TaskLists;

public class TaskListsIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
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
        var expectedResults = new List<TaskListsReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var handler = new TaskListsQueryHandler(database.DataSource);
        var query = new TaskListsQuery();
        await SetupDatabase(expectedResults);

        var results = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(results);
        foreach (var expectedResult in expectedResults)
        {
            Assert.Contains(results, result => result.Id == expectedResult.Id);
        }
    }

    private async Task SetupDatabase(List<TaskListsReadModel> expectedResults)
    {
        var projection = new TaskListsProjection(database.DataSource);

        foreach (var readModel in expectedResults)
        {
            await projection.Create(readModel, CancellationToken.None);
        }
    }

    private static TaskListsReadModel GenerateReadModel() => StateBuilder.Build<TaskListsReadModel>(
        new TaskListAdded(Generate.Guid(), Generate.String())
    );
}
