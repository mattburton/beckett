using System.Reflection;

namespace Beckett;

/// <summary>
/// Beckett modules provide a way to manage configuration for each application module. They support a client-server
/// architecture allowing you to supply separate client and server configuration for each module, as well as shared
/// message type configuration which is always applied regardless of the mode of operation. Modules can be configured
/// explicitly via <see cref="BeckettConfigurationExtensions.WithClientConfiguration{T}"/> and
/// <see cref="BeckettConfigurationExtensions.WithServerConfiguration{T}"/> or can be discovered using assembly scanning
/// via <see cref="BeckettConfigurationExtensions.WithClientConfigurationFrom"/> and
/// <see cref="BeckettConfigurationExtensions.WithServerConfigurationFrom"/> where you specify one or more assemblies to
/// scan for <see cref="IBeckettModule"/> implementations.
/// </summary>
public interface IBeckettModule
{
    /// <summary>
    /// Message type configuration for the module. This method will be run for both client and server configurations.
    /// </summary>
    /// <param name="builder"></param>
    void MessageTypes(IBeckettBuilder builder);

    /// <summary>
    /// Client-specific configuration for the module. In a client-server architecture, for example API + Worker, this
    /// method would be used to configure the system as needed to run as a client.
    /// </summary>
    /// <param name="builder"></param>
    void ClientConfiguration(IBeckettBuilder builder);

    /// <summary>
    /// Server-specific configuration for the module. In a client-server architecture, for example API + Worker, this
    /// method would be used to configure the system as needed to run as a server.
    /// </summary>
    /// <param name="builder"></param>
    void ServerConfiguration(IBeckettBuilder builder);
}

public static class BeckettConfigurationExtensions
{
    private static readonly Type ModuleType = typeof(IBeckettModule);

    /// <summary>
    /// Apply message type and client configuration from the specified <see cref="IBeckettModule"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IBeckettBuilder WithClientConfiguration<T>(this IBeckettBuilder builder)
        where T : IBeckettModule, new()
    {
        var module = new T();

        module.MessageTypes(builder);

        module.ClientConfiguration(builder);

        return builder;
    }

    /// <summary>
    /// Scan the provided assemblies for <see cref="IBeckettModule"/> implementations and apply message type and client
    /// configuration for each one found
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IBeckettBuilder WithClientConfigurationFrom(
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

                module.ClientConfiguration(builder);
            }
        }

        return builder;
    }

    /// <summary>
    /// Apply message type and server configuration from the specified <see cref="IBeckettModule"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IBeckettBuilder WithServerConfiguration<T>(this IBeckettBuilder builder)
        where T : IBeckettModule, new()
    {
        var module = new T();

        module.MessageTypes(builder);

        module.ServerConfiguration(builder);

        return builder;
    }

    /// <summary>
    /// Scan the provided assemblies for <see cref="IBeckettModule"/> implementations and apply message type and server
    /// configuration for each one found
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IBeckettBuilder WithServerConfigurationFrom(
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

                module.ServerConfiguration(builder);
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
