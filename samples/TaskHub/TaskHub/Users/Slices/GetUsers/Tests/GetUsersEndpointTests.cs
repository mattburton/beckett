using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUsers.Tests;

public class GetUsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryDispatcher = new FakeQueryExecutor();
            var expectedQuery = new GetUsersQuery();
            var expectedResults = new List<GetUsersReadModel>
            {
                StateBuilder.Build<GetUsersReadModel>(new UserRegistered("username", "email"))
            };
            queryDispatcher.Returns(expectedQuery, expectedResults);

            var result = await GetUsersEndpoint.Handle(queryDispatcher, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<IReadOnlyList<GetUsersReadModel>>>(result);
            Assert.Equal(expectedResults, actualResults.Value);
        }
    }
}
