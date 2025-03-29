using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Extensions;
using Core.Streams;
using TaskHub;
using Xunit.Sdk;

namespace Tests;

public class ScenarioTests
{
    [Fact]
    public async Task all_scenarios_pass()
    {
        var sources = TaskHubAssembly.Instance.GetLoadableTypes()
            .Where(t => typeof(IHaveScenarios).IsAssignableFrom(t))
            .Select(RuntimeHelpers.GetUninitializedObject)
            .Cast<IHaveScenarios>()
            .ToList();

        foreach (var source in sources)
        {
            var type = source.GetType();
            var name = type.Name;

            foreach (var scenario in source.Scenarios)
            {
                if (scenario is not End end)
                {
                    continue;
                }

                ScenarioParameters parameters = end;

                if (parameters.Command != null)
                {
                    await ProcessCommandScenario(type, parameters, name);
                }
                else
                {
                    ProcessStateScenario(parameters, name);
                }
            }
        }
    }

    private static void ProcessStateScenario(ScenarioParameters parameters, string name)
    {
        if (parameters.History.Length == 0)
        {
            Assert.Fail($"{name} scenario \"{parameters.Name}\" is missing event history");
        }

        try
        {
            var expected = JsonSerializer.Serialize(parameters.Expected);
            var actual = JsonSerializer.Serialize(parameters.Actual);

            Assert.Equal(expected, actual);
        }
        catch (XunitException e)
        {
            throw new AggregateException($"{name} scenario \"{parameters.Name}\" failed", e);
        }
    }

    private static async Task ProcessCommandScenario(Type type, ScenarioParameters parameters, string name)
    {
        if (Activator.CreateInstance(type) is not ICommandHandlerDispatcher dispatcher)
        {
            return;
        }

        var reader = new FakeStreamReader();

        if (parameters.History.Length > 0)
        {
            if (dispatcher is not IHaveStreamName streamNameProvider)
            {
                return;
            }

            var streamName = streamNameProvider.StreamName(parameters.Command!);

            reader.HasExistingStream(streamName, parameters.History);
        }

        try
        {
            var result = await dispatcher.Dispatch(
                parameters.Command!,
                reader,
                CancellationToken.None
            );

            Assert.Equivalent(parameters.Events, result.Events, true);
        }
        catch (XunitException e)
        {
            throw new AggregateException($"{name} scenario \"{parameters.Name}\" failed", e);
        }
        catch (Exception e)
        {
            if (parameters.ExceptionType == null)
            {
                Assert.Fail($"Unexpected exception was thrown by {name} during \"{parameters.Name}\": {e}");
            }

            Assert.IsType(parameters.ExceptionType, e);
        }
    }
}
