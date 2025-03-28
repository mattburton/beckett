using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Commands;
using TaskHub.TaskLists.Events;

namespace Tests.TaskLists.Commands;

public class ChangeTaskListNameCommandHandlerTests :
    CommandHandlerFixture<ChangeTaskListNameCommand, ChangeTaskListNameCommandHandler>
{
    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new ChangeTaskListNameCommand(taskListId, name)
            )
            .Then(
                new TaskListNameChanged(taskListId, name)
            );
    }
}
