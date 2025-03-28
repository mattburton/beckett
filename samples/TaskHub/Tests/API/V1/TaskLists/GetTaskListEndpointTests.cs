using API.V1.TaskLists;
using Contracts.TaskLists.Queries;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

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
            var query = new GetTaskListQuery(id);
            var expectedResult = new GetTaskListQuery.Result(id, name, [new GetTaskListQuery.TaskItem(task, false)]);
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new TaskListModule(commandDispatcher, queryDispatcher);
            queryDispatcher.Returns(query, expectedResult);

            var result = await GetTaskListEndpoint.Handle(id, module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetTaskListQuery.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var id = Generate.Guid();
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new TaskListModule(commandDispatcher, queryDispatcher);

            var result = await GetTaskListEndpoint.Handle(id, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
