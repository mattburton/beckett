using Beckett;
using Beckett.Messages;
using Core.Projections;

namespace Core.Tests.Projections;

public partial class ProjectionHandlerTests
{
    [Fact]
    public async Task creates_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestCreateMessage(expectedId))
        };

        await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
            projection,
            batch,
            CancellationToken.None
        );

        Assert.Contains(expectedId, projection.Results.Keys);
    }

    [Fact]
    public async Task updates_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestUpdateMessage(expectedId))
        };
        projection.Results.Add(expectedId, new TestReadModel
        {
            Id = expectedId,
        });

        await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
            projection,
            batch,
            CancellationToken.None
        );

        Assert.NotNull(projection.Results[expectedId]);
        Assert.True(projection.Results[expectedId].Updated);
    }

    [Fact]
    public async Task deletes_projection()
    {
        var expectedId = Guid.NewGuid();
        var projection = new TestReadModel.Projection();
        var batch = new List<IMessageContext>
        {
            MessageContext.From(new TestDeleteMessage(expectedId))
        };
        projection.Results.Add(expectedId, new TestReadModel
        {
            Id = expectedId,
        });

        await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
            projection,
            batch,
            CancellationToken.None
        );

        Assert.DoesNotContain(expectedId, projection.Results.Keys);
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
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestCreateOrUpdateMessage(expectedId))
                };

                await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
                    projection,
                    batch,
                    CancellationToken.None
                );

                Assert.Contains(expectedId, projection.Results.Keys);
            }
        }

        public class when_projection_exists
        {
            [Fact]
            public async Task updates_projection()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestCreateOrUpdateMessage(expectedId))
                };
                projection.Results.Add(expectedId, new TestReadModel
                {
                    Id = expectedId,
                });

                await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
                    projection,
                    batch,
                    CancellationToken.None
                );

                Assert.True(projection.Results[expectedId].Updated);
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
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestUpdateMessageWithIgnoreWhenNotFound(expectedId))
                };
                projection.Results.Add(expectedId, new TestReadModel
                {
                    Id = expectedId,
                });

                await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
                    projection,
                    batch,
                    CancellationToken.None
                );

                Assert.True(projection.Results[expectedId].Updated);
            }
        }

        public class when_projection_does_not_exist
        {
            [Fact]
            public async Task creates_projection_for_update()
            {
                var expectedId = Guid.NewGuid();
                var projection = new TestReadModel.Projection();
                var batch = new List<IMessageContext>
                {
                    MessageContext.From(new TestUpdateMessageWithIgnoreWhenNotFound(expectedId))
                };

                await ProjectionHandler<TestReadModel.Projection, TestReadModel, Guid>.Handle(
                    projection,
                    batch,
                    CancellationToken.None
                );

                Assert.Contains(expectedId, projection.Results.Keys);
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
            var expectedKey1 = ReadModelWithCompositeKey.IdFor(1, today);
            var expectedKey2 = ReadModelWithCompositeKey.IdFor(2, today);
            var batch = new List<IMessageContext>
            {
                MessageContext.From(new TestMessageWithCompositeKey(1, today)),
                MessageContext.From(new TestMessageWithCompositeKey(2, today))
            };

            await ProjectionHandler<ReadModelWithCompositeKey.Projection, ReadModelWithCompositeKey, string>.Handle(
                projection,
                batch,
                CancellationToken.None
            );

            Assert.Contains(expectedKey1, projection.Results.Keys);
            Assert.Contains(expectedKey2, projection.Results.Keys);
        }
    }

    [State]
    public partial class TestReadModel
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

        public class Projection : IProjection<TestReadModel, Guid>
        {
            public readonly Dictionary<Guid, TestReadModel> Results = [];

            public void Configure(IProjectionConfiguration<Guid> configuration)
            {
                configuration.CreatedBy<TestCreateMessage>(x => x.Id);
                configuration.CreatedOrUpdatedBy<TestCreateOrUpdateMessage>(x => x.Id);
                configuration.UpdatedBy<TestUpdateMessage>(x => x.Id);
                configuration.UpdatedBy<TestUpdateMessageWithIgnoreWhenNotFound>(x => x.Id).IgnoreWhenNotFound();
                configuration.DeletedBy<TestDeleteMessage>(x => x.Id);
            }

            public Task Create(TestReadModel state, CancellationToken cancellationToken)
            {
                Results.Add(state.Id, state);

                return Task.CompletedTask;
            }

            public Task<TestReadModel?> Read(Guid key, CancellationToken cancellationToken)
            {
                return Task.FromResult(Results.GetValueOrDefault(key));
            }

            public Task Update(TestReadModel state, CancellationToken cancellationToken)
            {
                Results[state.Id] = state;

                return Task.CompletedTask;
            }

            public Task Delete(Guid key, CancellationToken cancellationToken)
            {
                Results.Remove(key);

                return Task.CompletedTask;
            }
        }
    }

    [State]
    public partial class ReadModelWithCompositeKey
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

        public class Projection : IProjection<ReadModelWithCompositeKey, string>
        {
            public readonly Dictionary<string, ReadModelWithCompositeKey> Results = [];

            public void Configure(IProjectionConfiguration<string> configuration)
            {
                configuration.CreatedBy<TestMessageWithCompositeKey>(x => IdFor(x.Id, x.Date));
            }

            public Task Create(ReadModelWithCompositeKey state, CancellationToken cancellationToken)
            {
                Results.Add(state.CompositeKey, state);

                return Task.CompletedTask;
            }

            public Task<ReadModelWithCompositeKey?> Read(string key, CancellationToken cancellationToken)
            {
                return Task.FromResult(Results.GetValueOrDefault(key));
            }

            public Task Update(ReadModelWithCompositeKey state, CancellationToken cancellationToken)
            {
                Results[state.CompositeKey] = state;

                return Task.CompletedTask;
            }

            public Task Delete(string key, CancellationToken cancellationToken)
            {
                Results.Remove(key);

                return Task.CompletedTask;
            }
        }
    }

    public record TestCreateMessage(Guid Id);
    public record TestCreateOrUpdateMessage(Guid Id);
    public record TestUpdateMessage(Guid Id);
    public record TestUpdateMessageWithIgnoreWhenNotFound(Guid Id);
    public record TestDeleteMessage(Guid Id);
    public record TestMessageWithCompositeKey(int Id, DateOnly Date);
}
