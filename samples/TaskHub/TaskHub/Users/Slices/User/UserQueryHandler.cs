using TaskHub.Users.Contracts.Queries;

namespace TaskHub.Users.Slices.User;

public class UserQueryHandler(
    IMessageStore messageStore
) : ProjectedStreamQueryHandler<UserQuery, UserReadModel>(messageStore)
{
    protected override string StreamName(UserQuery query) => UserModule.StreamName(query.Username);

    public override async Task<UserReadModel?> Handle(UserQuery query, CancellationToken cancellationToken)
    {
        var result = await base.Handle(query, cancellationToken);

        return result is { Deleted: false } ? result : null;
    }
}
