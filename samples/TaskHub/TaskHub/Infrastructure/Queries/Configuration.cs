using TaskHub.Infrastructure.DependencyInjection;

namespace TaskHub.Infrastructure.Queries;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IQueryExecutor, QueryExecutor>();

        services.Scan(
            x => x.FromCallingAssembly().AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithTransientLifetime()
        );
    }
}
