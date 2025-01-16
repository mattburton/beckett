using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.Users.Tests;

public class UsersIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new UsersProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);

        Assert.NotNull(readModel);
        Assert.Equal(state.Username, readModel.Username);
        Assert.Equal(state.Email, readModel.Email);
    }

    [Fact]
    public async Task deletes_read_model()
    {
        var state = GenerateReadModel();
        var projection = new UsersProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Username, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_all_rows()
    {
        var expectedResults = new List<UsersReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var handler = new UsersQueryHandler(database.DataSource);
        var query = new UsersQuery();
        await SetupDatabase(expectedResults);

        var results = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(results);
        foreach (var expectedResult in expectedResults)
        {
            Assert.Contains(results, result => result.Username == expectedResult.Username);
        }
    }

    private async Task SetupDatabase(List<UsersReadModel> expectedResults)
    {
        var projection = new UsersProjection(database.DataSource);

        foreach (var readModel in expectedResults)
        {
            await projection.Create(readModel, CancellationToken.None);
        }
    }

    private static UsersReadModel GenerateReadModel() => StateBuilder.Build<UsersReadModel>(
        new UserRegistered(Generate.String(), Generate.String())
    );
}
