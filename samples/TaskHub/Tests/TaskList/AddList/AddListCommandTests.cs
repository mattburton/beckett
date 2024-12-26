using TaskHub.TaskList.AddList;
using TaskHub.TaskList.Events;

namespace Tests.TaskList.AddList;

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
