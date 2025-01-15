using System.Runtime.CompilerServices;
using Beckett.Projections;

namespace Beckett.Tests.Projections;

public partial class ProjectionConfigurationTests
{
    public class when_configured_correctly
    {
        [Fact]
        public void is_valid()
        {
            var projection =
                (IProjection<TestReadModel, Guid>)RuntimeHelpers.GetUninitializedObject(
                    typeof(TestReadModelProjection)
                );

            var configuration = new ProjectionConfiguration<Guid>();

            projection.Configure(configuration);

            try
            {
                configuration.Validate(new TestReadModel());
            }
            catch
            {
                Assert.Fail("Projection configuration should be valid");
            }
        }
    }

    public class when_message_type_is_applied_but_not_configured
    {
        [Fact]
        public void throws()
        {
            var projection =
                (IProjection<TestReadModel, Guid>)RuntimeHelpers.GetUninitializedObject(
                    typeof(TestReadModelProjectionMissingConfiguration)
                );

            var configuration = new ProjectionConfiguration<Guid>();

            projection.Configure(configuration);

            var exception = Assert.Throws<Exception>(() => configuration.Validate(new TestReadModel()));
            Assert.Contains("Applied but not configured", exception.Message);
            Assert.Contains(typeof(TestUpdateMessage).FullName!, exception.Message);
        }
    }

    public class when_message_type_is_configured_but_not_applied
    {
        [Fact]
        public void throws()
        {
            var projection =
                (IProjection<TestReadModel, Guid>)RuntimeHelpers.GetUninitializedObject(
                    typeof(TestReadModelProjectionMissingApplyMethod)
                );

            var configuration = new ProjectionConfiguration<Guid>();

            projection.Configure(configuration);

            var exception = Assert.Throws<Exception>(() => configuration.Validate(new TestReadModel()));
            Assert.Contains("Configured but not applied", exception.Message);
            Assert.Contains(typeof(TestUpdateMessageNotApplied).FullName!, exception.Message);
        }
    }

    [State]
    public partial class TestReadModel
    {
        private void Apply(TestCreateMessage _)
        {
        }

        private void Apply(TestUpdateMessage _)
        {
        }
    }

    public class TestReadModelProjection : IProjection<TestReadModel, Guid>
    {
        public void Configure(IProjectionConfiguration<Guid> configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id);
            configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
        }

        public Task Create(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TestReadModel?> Read(Guid key, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Update(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Delete(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    public class TestReadModelProjectionMissingConfiguration : IProjection<TestReadModel, Guid>
    {
        public void Configure(IProjectionConfiguration<Guid> configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id);
        }

        public Task Create(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TestReadModel?> Read(Guid key, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Update(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Delete(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    public class TestReadModelProjectionMissingApplyMethod : IProjection<TestReadModel, Guid>
    {
        public void Configure(IProjectionConfiguration<Guid> configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id);
            configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
            configuration.UpdatedBy<TestUpdateMessageNotApplied>(x => x.Id);
        }

        public Task Create(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TestReadModel?> Read(Guid key, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Update(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task Delete(TestReadModel state, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    public record TestCreateMessage(Guid Id);

    public record TestUpdateMessage(Guid Id);

    public record TestUpdateMessageNotApplied(Guid Id);
}