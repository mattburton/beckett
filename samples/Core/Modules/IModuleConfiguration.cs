namespace Core.Modules;

public interface IModuleConfiguration
{
    string ModuleName { get; }

    void Configure(IModuleBuilder builder);
}
