using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Commands;
using TaskHub.TaskLists.Events;

namespace Tests.TaskLists.Commands;

public class DeleteTaskListCommandHandlerTests :
    CommandHandlerFixture<DeleteTaskListCommand, DeleteTaskListCommandHandler>
{
    [Fact]
    public void task_list_deleted()
    {
        var id = Generate.Guid();

        Specification
            .When(
                new DeleteTaskListCommand(id)
            )
            .Then(
                new TaskListDeleted(id)
            );
    }
}
