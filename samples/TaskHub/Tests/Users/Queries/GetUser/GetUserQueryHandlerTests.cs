using Contracts.Users.Queries;
using Core.Streams;
using TaskHub.Users.Events;
using TaskHub.Users.Queries.GetUser;
using TaskHub.Users.Streams;

namespace Tests.Users.Queries.GetUser;

public class GetUserQueryHandlerTests
{
    public class when_user_exists
    {
        [Fact]
        public async Task returns_read_model()
        {
            var username = Generate.String();
            var email = Generate.String();
            var stream = new UserStream(username);
            var reader = new FakeStreamReader();
            var handler = new GetUserQueryHandler(reader);
            var query = new GetUserQuery(username);
            reader.HasExistingStream(stream, new UserRegistered(username, email));

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(email, result.Email);
        }
    }

    public class when_user_does_not_exist
    {
        [Fact]
        public async Task returns_null()
        {
            var username = Generate.String();
            var reader = new FakeStreamReader();
            var handler = new GetUserQueryHandler(reader);
            var query = new GetUserQuery(username);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
