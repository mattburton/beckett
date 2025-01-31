using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.TaskList.Tests;

public class GetTaskListEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var id = Generate.Guid();
            var name = Generate.String();
            var task = Generate.String();
            var query = new TaskListQuery(id);
            var expectedResult = StateBuilder.Build<TaskListReadModel>(
                new TaskListAdded(id, name),
                new TaskAdded(id, task)
            );
            var queryBus = new FakeQueryBus();
            queryBus.Returns(query, expectedResult);

            var result = await GetTaskListEndpoint.Handle(id, queryBus, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<TaskListReadModel>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var id = Generate.Guid();
            var queryBus = new FakeQueryBus();

            var result = await GetTaskListEndpoint.Handle(id, queryBus, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
