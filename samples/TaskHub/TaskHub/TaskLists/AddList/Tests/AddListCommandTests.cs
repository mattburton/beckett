using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.AddList.Tests;

public class AddListCommandTests : CommandSpecificationFixture<AddListCommand>
{
    [Fact]
    public void task_list_added()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();

        Specification
            .When(new AddListCommand(id, name))
            .Then(new TaskListAdded(id, name));
    }
}
