using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetLists.Tests;

public class GetListsEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryDispatcher = new FakeQueryDispatcher();
            var expectedQuery = new GetListsQuery();
            var expectedResults = new List<GetListsReadModel>
            {
                StateBuilder.Build<GetListsReadModel>(new TaskListAdded(Guid.NewGuid(), "name"))
            };
            queryDispatcher.Returns(expectedQuery, expectedResults);

            var result = await GetListsEndpoint.Handle(queryDispatcher, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<IReadOnlyList<GetListsReadModel>>>(result);
            Assert.Equal(expectedResults, actualResult.Value);
        }
    }
}
