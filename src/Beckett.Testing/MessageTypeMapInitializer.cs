using System.Reflection;
using Beckett.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Testing;

public static class MessageTypeMapInitializer
{
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
