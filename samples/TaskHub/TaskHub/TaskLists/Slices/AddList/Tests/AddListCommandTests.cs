using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.AddList.Tests;

public class AddListCommandTests : CommandSpecificationFixture<AddListCommand>
{
    [Fact]
    public void task_list_added()
    {
        var id = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new AddListCommand(id, name)
            )
            .Then(
                new TaskListAdded(id, name)
            );
    }
}
