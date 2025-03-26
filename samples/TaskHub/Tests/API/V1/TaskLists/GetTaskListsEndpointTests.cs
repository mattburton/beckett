using API.V1.TaskLists;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.TaskLists;

namespace Tests.API.V1.TaskLists;

public class GetTaskListsEndpointTests
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
                ReadModelBuilder.Build<TaskListsReadModel>(new TaskListAdded(Guid.NewGuid(), "name"))
            };
            queryBus.Returns(query, expectedResults);

            var result = await GetTaskListsEndpoint.Handle(queryBus, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<IReadOnlyList<TaskListsReadModel>>>(result);
            Assert.Equal(expectedResults, actualResult.Value);
        }
    }
}
