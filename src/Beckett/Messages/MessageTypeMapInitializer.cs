using System.Reflection;
using Beckett.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Messages;

public static class MessageTypeMapInitializer
{
    /// <summary>
    /// Initialize the <see cref="MessageTypeMap"/> using <see cref="IBeckettModule"/> implementations
    /// discovered in the provided list of assemblies. Mostly useful for global test setup for unit tests, however it
    /// can be used as an alternative means of setting up the system where appropriate.
    /// </summary>
    /// <param name="assemblies"></param>
    public static void Initialize(params Assembly[] assemblies)
    {
        var builder = new BeckettBuilder(new ServiceCollection());

        foreach (var assembly in assemblies)
        {
            var types = GetTypes(assembly).Where(IsModule);

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);

                if (instance is not IBeckettModule module)
                {
                    continue;
                }

                module.MessageTypes(builder);
            }
        }
    }

    private static Type[] GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            return [];
        }
    }

    private static bool IsModule(Type x) =>
        typeof(IBeckettModule).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false };
}
