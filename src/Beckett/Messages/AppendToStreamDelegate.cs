namespace Beckett.Messages;

public delegate Task<AppendResult> AppendToStreamDelegate(
    string streamName,
    ExpectedVersion expectedVersion,
    IEnumerable<object> messages,
    CancellationToken cancellationToken
);

public delegate void SessionAppendToStreamDelegate(
    string streamName,
    ExpectedVersion expectedVersion,
    IEnumerable<object> messages
);
