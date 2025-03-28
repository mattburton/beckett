using API.V1.TaskLists;
using Contracts.TaskLists.Queries;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class GetTaskListsEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var expectedResult = new GetTaskListsQuery.Result([new GetTaskListsQuery.TaskList(Guid.NewGuid(), "name")]);
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new TaskListModule(commandDispatcher, queryDispatcher);
            var query = new GetTaskListsQuery();
            queryDispatcher.Returns(query, expectedResult);

            var result = await GetTaskListsEndpoint.Handle(module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetTaskListsQuery.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }
}
