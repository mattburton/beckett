namespace Users.Tests;

public class ScenarioTests
{
    [Fact]
    public async Task execute_scenarios()
    {
        await ScenarioExecutor.Execute(typeof(IUserModule).Assembly);
    }
}
