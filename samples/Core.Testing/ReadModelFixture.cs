using Beckett;

namespace Core.Testing;

public abstract class ReadModelFixture<T> where T : class, IApply, new()
{
    protected ReadModelSpecification<T> Specification => new();
}
