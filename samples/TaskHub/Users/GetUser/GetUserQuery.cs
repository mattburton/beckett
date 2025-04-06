namespace Users.GetUser;

public record GetUserQuery(string Username) : IQuery<GetUserReadModel>
{
    public class Handler(IStreamReader reader) : StreamStateQueryHandler<GetUserQuery, GetUserReadModel>(reader)
    {
        protected override IStreamName StreamName(GetUserQuery query) => new UserStream(query.Username);
    }
}
