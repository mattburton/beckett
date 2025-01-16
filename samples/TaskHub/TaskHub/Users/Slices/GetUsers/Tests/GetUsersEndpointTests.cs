using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUsers.Tests;

public class GetUsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryDispatcher = new FakeQueryDispatcher();
            var expectedQuery = new GetUsersQuery();
            var expectedResults = new List<TaskLists.Slices.UserLookup.UserLookupReadModel>
            {
                StateBuilder.Build<TaskLists.Slices.UserLookup.UserLookupReadModel>(new UserRegistered("username", "email"))
            };
            queryDispatcher.Returns(expectedQuery, expectedResults);

            var result = await GetUsersEndpoint.Handle(queryDispatcher, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<IReadOnlyList<TaskLists.Slices.UserLookup.UserLookupReadModel>>>(result);
            Assert.Equal(expectedResults, actualResults.Value);
        }
    }
}
