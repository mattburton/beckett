namespace Beckett.Testing;

public abstract class StateSpecificationFixture<T> where T : IApply, new()
{
    protected StateSpecification<T> Specification => new();
}
