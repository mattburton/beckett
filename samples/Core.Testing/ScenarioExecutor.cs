using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Commands;
using Core.Extensions;
using Core.Scenarios;
using Core.Streams;
using Xunit;
using Xunit.Sdk;

namespace Core.Testing;

public static class ScenarioExecutor
{
    public static async Task Execute(params Assembly[] assemblies)
    {
        var sources = assemblies.SelectMany(x => x.GetLoadableTypes())
            .Where(t => typeof(IHaveScenarios).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .Select(RuntimeHelpers.GetUninitializedObject)
            .Cast<IHaveScenarios>()
            .ToList();

        foreach (var source in sources)
        {
            var name = source.GetType().Name;

            foreach (var scenario in source.Scenarios)
            {
                if (scenario is not End end)
                {
                    continue;
                }

                ScenarioParameters parameters = end;

                if (parameters.Command != null)
                {
                    await ProcessCommandScenario(parameters, name);
                }
                else
                {
                    ProcessStateViewScenario(parameters, name);
                }
            }
        }
    }

    private static void ProcessStateViewScenario(ScenarioParameters parameters, string stateViewName)
    {
        if (parameters.History.Length == 0)
        {
            Assert.Fail($"{stateViewName} scenario \"{parameters.Name}\" is missing event history");
        }

        try
        {
            var expected = JsonSerializer.Serialize(parameters.Expected);
            var actual = JsonSerializer.Serialize(parameters.Actual);

            Assert.Equal(expected, actual);
        }
        catch (XunitException e)
        {
            throw new AggregateException($"{stateViewName} scenario \"{parameters.Name}\" failed", e);
        }
    }

    private static async Task ProcessCommandScenario(ScenarioParameters parameters, string commandName)
    {
        var reader = new FakeStreamReader();

        if (parameters.History.Length > 0)
        {
            if (parameters.Command is not IHaveStreamName streamNameProvider)
            {
                return;
            }

            var streamName = streamNameProvider.StreamName();

            reader.HasExistingStream(streamName, parameters.History);
        }

        try
        {
            var result = await parameters.Command!.Dispatch(
                parameters.Command,
                reader,
                CancellationToken.None
            );

            Assert.Equivalent(parameters.Events, result.Events, true);
        }
        catch (XunitException e)
        {
            throw new AggregateException($"{commandName} scenario \"{parameters.Name}\" failed", e);
        }
        catch (Exception e)
        {
            if (parameters.ExceptionType == null)
            {
                Assert.Fail($"Unexpected exception was thrown by {commandName} during \"{parameters.Name}\" scenario: {e}");
            }

            Assert.IsType(parameters.ExceptionType, e);
        }
    }
}
