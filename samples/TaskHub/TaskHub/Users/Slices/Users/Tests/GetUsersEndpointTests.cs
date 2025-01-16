using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.Users.Tests;

public class GetUsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryDispatcher = new FakeQueryDispatcher();
            var expectedQuery = new UsersQuery();
            var expectedResults = new List<UsersReadModel>
            {
                StateBuilder.Build<UsersReadModel>(new UserRegistered("username", "email"))
            };
            queryDispatcher.Returns(expectedQuery, expectedResults);

            var result = await GetUsersEndpoint.Handle(queryDispatcher, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<IReadOnlyList<UsersReadModel>>>(result);
            Assert.Equal(expectedResults, actualResults.Value);
        }
    }
}
