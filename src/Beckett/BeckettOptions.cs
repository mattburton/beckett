using System.Reflection;
using Beckett.Storage.Postgres;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    internal Assembly[] Assemblies { get; private set; } = AppDomain.CurrentDomain.GetAssemblies();
    internal PostgresOptions Postgres { get; } = new();

    public SubscriptionOptions Subscriptions { get; } = new();

    public void UseAssemblies(params Assembly[] assemblies)
    {
        Assemblies = assemblies;
    }

    public void UsePostgres(Action<PostgresOptions> configure)
    {
        Postgres.Enabled = true;

        configure(Postgres);
    }

    internal IEnumerable<Type> GetAssemblyTypes()
    {
        return Assemblies.SelectMany(x =>
        {
            try
            {
                return x.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        });
    }
}
