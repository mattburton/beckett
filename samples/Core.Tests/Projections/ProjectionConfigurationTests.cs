using Beckett;
using Core.Projections;
using Core.Scenarios;
using Core.State;

namespace Core.Tests.Projections;

public partial class ProjectionConfigurationTests
{
    public class when_configured_correctly
    {
        [Fact]
        public void is_valid()
        {
            var projection = new TestReadModelProjection();
            var configuration = new ProjectionConfiguration();

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
            var projection = new TestReadModelProjectionMissingConfiguration();
            var configuration = new ProjectionConfiguration();

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
            var projection = new TestReadModelProjectionMissingApplyMethod();
            var configuration = new ProjectionConfiguration();

            projection.Configure(configuration);

            var exception = Assert.Throws<Exception>(() => configuration.Validate(new TestReadModel()));
            Assert.Contains("Configured but not applied", exception.Message);
            Assert.Contains(typeof(TestUpdateMessageNotApplied).FullName!, exception.Message);
        }
    }

    public class when_where_predicate_is_configured
    {
        [Fact]
        public void includes_messages_matching_predicate()
        {
            var projection = new TestReadModelProjection();
            var configuration = new ProjectionConfiguration();
            projection.Configure(configuration);
            var batch = new List<IMessageContext>
            {
                MessageContext.From(new TestCreateMessage(Guid.NewGuid()))
            };

            var filteredBatch = configuration.Filter(batch);

            Assert.Single(filteredBatch);
        }

        [Fact]
        public void excludes_messages_that_do_not_match_predicate()
        {
            var projection = new TestReadModelProjection();
            var configuration = new ProjectionConfiguration();
            projection.Configure(configuration);
            var batch = new List<IMessageContext>
            {
                MessageContext.From(new TestCreateMessage(Guid.Empty))
            };

            var filteredBatch = configuration.Filter(batch);

            Assert.Empty(filteredBatch);
        }
    }

    [State]
    public partial class TestReadModel : IStateView
    {
        private void Apply(TestCreateMessage _)
        {
        }

        private void Apply(TestUpdateMessage _)
        {
        }

        public IScenario[] Scenarios => [];
    }

    public class TestReadModelProjection : IProjection<TestReadModel>
    {
        public void Configure(IProjectionConfiguration configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id).Where(x => x.Id != Guid.Empty);
            configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
        }

        public Task<IReadOnlyList<TestReadModel>> Load(IEnumerable<object> keys, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public object GetKey(TestReadModel state) => throw new NotImplementedException();

        public void Save(TestReadModel state) => throw new NotImplementedException();

        public void Delete(TestReadModel state) => throw new NotImplementedException();

        public Task SaveChanges(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class TestReadModelProjectionMissingConfiguration : IProjection<TestReadModel>
    {
        public void Configure(IProjectionConfiguration configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id);
        }

        public Task<IReadOnlyList<TestReadModel>> Load(IEnumerable<object> keys, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public object GetKey(TestReadModel state) => throw new NotImplementedException();

        public void Save(TestReadModel state) => throw new NotImplementedException();

        public void Delete(TestReadModel state) => throw new NotImplementedException();

        public Task SaveChanges(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class TestReadModelProjectionMissingApplyMethod : IProjection<TestReadModel>
    {
        public void Configure(IProjectionConfiguration configuration)
        {
            configuration.CreatedBy<TestCreateMessage>(x => x.Id);
            configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
            configuration.UpdatedBy<TestUpdateMessageNotApplied>(x => x.Id);
        }

        public Task<IReadOnlyList<TestReadModel>> Load(IEnumerable<object> keys, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public object GetKey(TestReadModel state) => throw new NotImplementedException();

        public void Save(TestReadModel state) => throw new NotImplementedException();

        public void Delete(TestReadModel state) => throw new NotImplementedException();

        public Task SaveChanges(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public record TestCreateMessage(Guid Id);

    public record TestUpdateMessage(Guid Id);

    public record TestUpdateMessageNotApplied(Guid Id);
}
