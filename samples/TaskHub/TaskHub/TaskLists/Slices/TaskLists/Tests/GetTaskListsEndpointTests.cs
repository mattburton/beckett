using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.TaskLists.Tests;

public class GetTaskListsEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryDispatcher = new FakeQueryDispatcher();
            var expectedQuery = new TaskListsQuery();
            var expectedResults = new List<TaskListsReadModel>
            {
                StateBuilder.Build<TaskListsReadModel>(new TaskListAdded(Guid.NewGuid(), "name"))
            };
            queryDispatcher.Returns(expectedQuery, expectedResults);

            var result = await GetTaskListsEndpoint.Handle(queryDispatcher, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<IReadOnlyList<TaskListsReadModel>>>(result);
            Assert.Equal(expectedResults, actualResult.Value);
        }
    }
}
