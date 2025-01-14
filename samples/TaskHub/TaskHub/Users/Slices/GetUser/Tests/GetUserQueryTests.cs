using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUser.Tests;

public class GetUserQueryTests
{
    public class when_user_exists
    {
        [Fact]
        public async Task returns_read_model()
        {
            var username = Generate.String();
            var email = Generate.String();
            var streamName = UserModule.StreamName(username);
            var messageStore = new FakeMessageStore();
            var handler = new GetUserQuery.Handler(messageStore);
            var query = new GetUserQuery(username);
            messageStore.HasExistingMessages(streamName, new UserRegistered(username, email));

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
            var messageStore = new FakeMessageStore();
            var handler = new GetUserQuery.Handler(messageStore);
            var query = new GetUserQuery(username);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
