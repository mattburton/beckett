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
            var dispatcher = new FakeDispatcher();
            var module = new TaskListModule(dispatcher);
            var query = new GetTaskLists();
            var expectedResult = new GetTaskLists.Result(
                [
                    new GetTaskLists.TaskList(Example.Guid, Example.String)
                ]
            );
            dispatcher.Returns(query, expectedResult);

            var result = await GetTaskListsEndpoint.Handle(module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetTaskLists.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }
}
