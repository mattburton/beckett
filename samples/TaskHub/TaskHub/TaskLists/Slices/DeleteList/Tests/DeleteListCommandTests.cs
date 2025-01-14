using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.DeleteList.Tests;

public class DeleteListCommandTests : CommandSpecificationFixture<DeleteListCommand>
{
    [Fact]
    public void task_list_deleted()
    {
        var id = Generate.Guid();

        Specification
            .When(
                new DeleteListCommand(id)
            )
            .Then(
                new TaskListDeleted(id)
            );
    }
}
