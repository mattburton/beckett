using TaskHub.Users.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup.Tests;

public class UserLookupIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new UserLookupProjection(database.DataSource);
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
        var projection = new UserLookupProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Username, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_matching_row()
    {
        var expectedResult = GenerateReadModel();
        var handler = new UserLookupQueryHandler(database.DataSource);
        var query = new UserLookupQuery(expectedResult.Username);
        await SetupDatabase([expectedResult]);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equivalent(expectedResult, result);
    }

    private async Task SetupDatabase(List<UserLookupReadModel> expectedResults)
    {
        var projection = new UserLookupProjection(database.DataSource);

        foreach (var readModel in expectedResults)
        {
            await projection.Create(readModel, CancellationToken.None);
        }
    }

    private static UserLookupReadModel GenerateReadModel() => StateBuilder.Build<UserLookupReadModel>(
        new User(Operation.Create, Generate.String(), Generate.String())
    );
}
