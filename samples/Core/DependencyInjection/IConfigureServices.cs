using Microsoft.Extensions.DependencyInjection;

namespace Core.DependencyInjection;

public interface IConfigureServices
{
    void Services(IServiceCollection services);
}
