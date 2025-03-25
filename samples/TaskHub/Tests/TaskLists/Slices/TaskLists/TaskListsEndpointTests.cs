using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.TaskLists;

namespace Tests.TaskLists.Slices.TaskLists;

public class TaskListsEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryBus = new FakeQueryBus();
            var query = new TaskListsQuery();
            var expectedResults = new List<TaskListsReadModel>
            {
                StateBuilder.Build<TaskListsReadModel>(new TaskListAdded(Guid.NewGuid(), "name"))
            };
            queryBus.Returns(query, expectedResults);

            var result = await TaskListsEndpoint.Handle(queryBus, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<IReadOnlyList<TaskListsReadModel>>>(result);
            Assert.Equal(expectedResults, actualResult.Value);
        }
    }
}
