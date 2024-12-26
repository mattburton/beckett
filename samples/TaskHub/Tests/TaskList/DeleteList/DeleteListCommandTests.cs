using TaskHub.TaskList.DeleteList;
using TaskHub.TaskList.Events;

namespace Tests.TaskList.DeleteList;

public class DeleteListCommandTests : CommandSpecificationFixture<DeleteListCommand>
{
    [Fact]
    public void task_list_deleted()
    {
        var id = Guid.NewGuid();

        Specification
            .When(new DeleteListCommand(id))
            .Then(new TaskListDeleted(id));
    }
}
