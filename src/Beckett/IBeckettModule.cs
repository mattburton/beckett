using System.Reflection;

namespace Beckett;

/// <summary>
/// Beckett modules provide a way to manage configuration for each application module. Message types are configured
/// separately from subscriptions to permit client-server architectures where only the message types are configured on
/// the client, and message types + subscriptions on the server.
/// </summary>
public interface IBeckettModule
{
    /// <summary>
    /// Message type configuration for the module.
    /// </summary>
    /// <param name="builder"></param>
    void MessageTypes(IMessageTypeBuilder builder);

    /// <summary>
    /// Subscription configuration for the module.
    /// </summary>
    /// <param name="builder"></param>
    void Subscriptions(ISubscriptionBuilder builder);
}

public static class BeckettConfigurationExtensions
{
    private static readonly Type ModuleType = typeof(IBeckettModule);

    /// <summary>
    /// Configure message types using the specified <see cref="IBeckettModule"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IBeckettBuilder WithMessageTypes<T>(this IBeckettBuilder builder)
        where T : IBeckettModule, new()
    {
        var module = new T();

        module.MessageTypes(builder);

        return builder;
    }

    /// <summary>
    /// Scan the provided assemblies for <see cref="IBeckettModule"/> implementations and configure message types for
    /// each module
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IBeckettBuilder WithMessageTypesFrom(
        this IBeckettBuilder builder,
        params Assembly[] assemblies
    )
    {
        var assembliesToScan = AssembliesToScan(assemblies);

        foreach (var assembly in assembliesToScan)
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

        return builder;
    }

    /// <summary>
    /// Configure message types and subscriptions from the specified <see cref="IBeckettModule"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IBeckettBuilder WithSubscriptions<T>(this IBeckettBuilder builder)
        where T : IBeckettModule, new()
    {
        var module = new T();

        module.MessageTypes(builder);

        module.Subscriptions(builder);

        return builder;
    }

    /// <summary>
    /// Scan the provided assemblies for <see cref="IBeckettModule"/> implementations and configure message types and
    /// subscriptions for each module
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IBeckettBuilder WithSubscriptionsFrom(
        this IBeckettBuilder builder,
        params Assembly[] assemblies
    )
    {
        var assembliesToScan = AssembliesToScan(assemblies);

        foreach (var assembly in assembliesToScan)
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

                module.Subscriptions(builder);
            }
        }

        return builder;
    }

    private static bool IsModule(Type x) =>
        ModuleType.IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false };

    private static Assembly[] AssembliesToScan(Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly must be provided to scan for modules",
                nameof(assemblies)
            );
        }

        return assemblies;
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
}
