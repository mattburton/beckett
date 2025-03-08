using TaskHub.Infrastructure.DependencyInjection;

namespace TaskHub.Infrastructure.Queries;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IQueryBus, QueryBus>();

        services.Scan(
            x => x.FromAssemblyOf<Configuration>().AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithTransientLifetime()
        );
    }
}
