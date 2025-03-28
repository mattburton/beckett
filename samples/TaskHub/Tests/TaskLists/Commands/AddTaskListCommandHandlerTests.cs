using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Commands;
using TaskHub.TaskLists.Events;

namespace Tests.TaskLists.Commands;

public class AddTaskListCommandHandlerTests : CommandHandlerFixture<AddTaskListCommand, AddTaskListCommandHandler>
{
    [Fact]
    public void task_list_added()
    {
        var id = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new AddTaskListCommand(id, name)
            )
            .Then(
                new TaskListAdded(id, name)
            );
    }
}
