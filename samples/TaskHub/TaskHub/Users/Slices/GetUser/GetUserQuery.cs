namespace TaskHub.Users.Slices.GetUser;

public record GetUserQuery(string Username) : IQuery<GetUserReadModel>
{
    public class Handler(
        IMessageStore messageStore
    ) : ProjectedStreamQueryHandler<GetUserQuery, GetUserReadModel>(messageStore)
    {
        protected override string StreamName(GetUserQuery query) => UserModule.StreamName(query.Username);

        public override async Task<GetUserReadModel?> Handle(GetUserQuery query, CancellationToken cancellationToken)
        {
            var result = await base.Handle(query, cancellationToken);

            return result is { Deleted: false } ? result : null;
        }
    }
}
