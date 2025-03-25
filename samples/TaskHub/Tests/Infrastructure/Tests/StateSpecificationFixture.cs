namespace Tests.Infrastructure.Tests;

public abstract class StateSpecificationFixture<T> where T : class, IApply, new()
{
    protected StateSpecification<T> Specification => new();
}
