using Beckett;
using Core.Projections;
using Core.Scenarios;
using Core.State;

namespace Core.Tests.Projections;

public partial class ProjectorTests
{
    [Fact]
    public async Task creates_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var projector = new Projector<TestReadModel>(projection);
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestCreateMessage(expectedId))
        };

        await projector.Project(batch, CancellationToken.None);

        Assert.Contains(expectedId, projection.Records.Keys);
    }

    [Fact]
    public async Task updates_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var projector = new Projector<TestReadModel>(projection);
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestUpdateMessage(expectedId))
        };
        projection.Records.Add(
            expectedId,
            new TestReadModel
            {
                Id = expectedId,
            }
        );

        await projector.Project(batch, CancellationToken.None);

        Assert.NotNull(projection.Records[expectedId]);
        Assert.True(projection.Records[expectedId].Updated);
    }

    [Fact]
    public async Task deletes_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var projector = new Projector<TestReadModel>(projection);
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestDeleteMessage(expectedId))
        };
        projection.Records.Add(
            expectedId,
            new TestReadModel
            {
                Id = expectedId,
            }
        );

        await projector.Project(batch, CancellationToken.None);

        Assert.DoesNotContain(expectedId, projection.Records.Keys);
    }

    [Fact]
    public async Task returns_affected_records_in_result()
    {
        var expectedCreatedId = Guid.NewGuid();
        var expectedUpdatedId = Guid.NewGuid();
        var expectedDeletedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var projector = new Projector<TestReadModel>(projection);
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestCreateMessage(expectedCreatedId)),
            MessageContext.From(new TestUpdateMessage(expectedUpdatedId)),
            MessageContext.From(new TestDeleteMessage(expectedDeletedId))
        };
        projection.Records.Add(
            expectedCreatedId,
            new TestReadModel
            {
                Id = expectedCreatedId,
            }
        );
        projection.Records.Add(
            expectedUpdatedId,
            new TestReadModel
            {
                Id = expectedUpdatedId,
            }
        );
        projection.Records.Add(
            expectedDeletedId,
            new TestReadModel
            {
                Id = expectedDeletedId,
            }
        );

        var result = await projector.Project(batch, CancellationToken.None);

        Assert.Contains(expectedCreatedId, result.AddedOrUpdated.Select(x => x.Id));
        Assert.Contains(expectedUpdatedId, result.AddedOrUpdated.Select(x => x.Id));
        Assert.Contains(expectedDeletedId, result.Removed.Select(x => x.Id));
    }

    public class when_configured_to_create_or_update
    {
        public class when_projection_does_not_exist
        {
            [Fact]
            public async Task creates_projection()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var projector = new Projector<TestReadModel>(projection);
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestCreateOrUpdateMessage(expectedId))
                };

                await projector.Project(batch, CancellationToken.None);

                Assert.Contains(expectedId, projection.Records.Keys);
            }
        }

        public class when_projection_exists
        {
            [Fact]
            public async Task updates_projection()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var projector = new Projector<TestReadModel>(projection);
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestCreateOrUpdateMessage(expectedId))
                };
                projection.Records.Add(
                    expectedId,
                    new TestReadModel
                    {
                        Id = expectedId,
                    }
                );

                await projector.Project(batch, CancellationToken.None);

                Assert.True(projection.Records[expectedId].Updated);
            }
        }
    }

    public class when_configured_to_ignore_when_not_found
    {
        public class when_projection_exists
        {
            [Fact]
            public async Task updates_projection()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var projector = new Projector<TestReadModel>(projection);
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestUpdateMessageWithIgnoreWhenNotFound(expectedId))
                };
                projection.Records.Add(
                    expectedId,
                    new TestReadModel
                    {
                        Id = expectedId,
                    }
                );

                await projector.Project(batch, CancellationToken.None);

                Assert.True(projection.Records[expectedId].Updated);
            }
        }

        public class when_projection_does_not_exist
        {
            [Fact]
            public async Task creates_projection_for_update()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var projector = new Projector<TestReadModel>(projection);
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestUpdateMessageWithIgnoreWhenNotFound(expectedId))
                };

                await projector.Project(batch, CancellationToken.None);

                Assert.Contains(expectedId, projection.Records.Keys);
            }
        }
    }

    public class when_projection_can_produce_more_than_one_result_per_batch
    {
        [Fact]
        public async Task stores_multiple_results()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var projection = new ReadModelWithCompositeKey.Projection();
            var projector = new Projector<ReadModelWithCompositeKey>(projection);
            var expectedKey1 = ReadModelWithCompositeKey.IdFor(1, today);
            var expectedKey2 = ReadModelWithCompositeKey.IdFor(2, today);
            var batch = new List<IMessageContext>
            {
                MessageContext.From(new TestMessageWithCompositeKey(1, today)),
                MessageContext.From(new TestMessageWithCompositeKey(2, today))
            };

            await projector.Project(batch, CancellationToken.None);

            Assert.Contains(expectedKey1, projection.Records.Keys);
            Assert.Contains(expectedKey2, projection.Records.Keys);
        }
    }

    [State]
    public partial class TestReadModel : IStateView
    {
        public Guid Id { get; set; }
        public bool Updated { get; private set; }

        private void Apply(TestCreateMessage m)
        {
            Id = m.Id;
        }

        private void Apply(TestCreateOrUpdateMessage m)
        {
            Id = m.Id;
            Updated = true;
        }

        private void Apply(TestUpdateMessage m)
        {
            Id = m.Id;
            Updated = true;
        }

        private void Apply(TestUpdateMessageWithIgnoreWhenNotFound m)
        {
            Id = m.Id;
            Updated = true;
        }

        public class Projection : IProjection<TestReadModel>
        {
            public readonly Dictionary<Guid, TestReadModel> Records = [];

            public void Configure(IProjectionConfiguration configuration)
            {
                configuration.CreatedBy<TestCreateMessage>(x => x.Id);
                configuration.CreatedOrUpdatedBy<TestCreateOrUpdateMessage>(x => x.Id);
                configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
                configuration.UpdatedBy<TestUpdateMessageWithIgnoreWhenNotFound>(x => x.Id).IgnoreWhenNotFound();
                configuration.DeletedBy<TestDeleteMessage>(x => x.Id);
            }

            public Task<IReadOnlyList<TestReadModel>> Load(IEnumerable<object> keys, CancellationToken cancellationToken)
            {
                var keyArray = keys.ToArray();

                IReadOnlyList<TestReadModel> results = Records.Where(x => keyArray.Contains(x.Key))
                    .Select(x => x.Value)
                    .ToList();

                return Task.FromResult(results);
            }

            public object GetKey(TestReadModel state) => state.Id;

            public void Save(TestReadModel state)
            {
                Records[state.Id] = state;
            }

            public void Delete(TestReadModel state)
            {
                Records.Remove(state.Id);
            }

            public Task SaveChanges(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public IScenario[] Scenarios => [];
    }

    [State]
    public partial class ReadModelWithCompositeKey : IStateView
    {
        private string CompositeKey => IdFor(Id, Date);
        private int Id { get; set; }
        private DateOnly Date { get; set; }

        public static string IdFor(int id, DateOnly date) => $"{id}#{date}";

        private void Apply(TestMessageWithCompositeKey m)
        {
            Id = m.Id;
            Date = m.Date;
        }

        public class Projection : IProjection<ReadModelWithCompositeKey>
        {
            public readonly Dictionary<string, ReadModelWithCompositeKey> Records = [];

            public void Configure(IProjectionConfiguration configuration)
            {
                configuration.CreatedBy<TestMessageWithCompositeKey>(x => IdFor(x.Id, x.Date));
            }

            public Task<IReadOnlyList<ReadModelWithCompositeKey>> Load(
                IEnumerable<object> keys,
                CancellationToken cancellationToken
            )
            {
                var keyArray = keys.ToArray();

                IReadOnlyList<ReadModelWithCompositeKey> results = Records.Where(x => keyArray.Contains(x.Key))
                    .Select(x => x.Value)
                    .ToList();

                return Task.FromResult(results);
            }

            public object GetKey(ReadModelWithCompositeKey state) => IdFor(state.Id, state.Date);

            public void Save(ReadModelWithCompositeKey state)
            {
                Records[state.CompositeKey] = state;
            }

            public void Delete(ReadModelWithCompositeKey state)
            {
                Records.Remove(state.CompositeKey);
            }

            public Task SaveChanges(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public IScenario[] Scenarios => [];
    }

    public record TestCreateMessage(Guid Id);

    public record TestCreateOrUpdateMessage(Guid Id);

    public record TestUpdateMessage(Guid Id);

    public record TestUpdateMessageWithIgnoreWhenNotFound(Guid Id);

    public record TestDeleteMessage(Guid Id);

    public record TestMessageWithCompositeKey(int Id, DateOnly Date);
}
