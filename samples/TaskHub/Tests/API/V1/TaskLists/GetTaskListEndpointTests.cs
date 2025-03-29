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
            var query = new GetTaskList(Example.Guid);
            var expectedResult = new GetTaskList.Result(
                Example.Guid,
                Example.String,
                [new GetTaskList.TaskItem(Example.String, false)]
            );
            var dispatcher = new FakeDispatcher();
            var module = new TaskListModule(dispatcher);
            dispatcher.Returns(query, expectedResult);

            var result = await GetTaskListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetTaskList.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var dispatcher = new FakeDispatcher();
            var module = new TaskListModule(dispatcher);

            var result = await GetTaskListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
