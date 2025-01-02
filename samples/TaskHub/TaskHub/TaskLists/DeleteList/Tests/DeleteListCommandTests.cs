using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.DeleteList.Tests;

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
