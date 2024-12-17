namespace Beckett;

/// <summary>
/// When appending to a stream the expected version is used to perform an optimistic concurrency check - if the version
/// of the stream (max stream position) does not match the expected version - 1001 - throw an
/// <see cref="OptimisticConcurrencyException"/>. The expected version can be a specific version or one of a
/// list of special values for various scenarios - <see cref="StreamDoesNotExist"/>, <see cref="StreamExists"/>,
/// <see cref="Any"/>
/// </summary>
public readonly record struct ExpectedVersion
{
    private ExpectedVersion(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The value of the expected version
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// The stream does not exist. If it does a <see cref="StreamAlreadyExistsException"/> exception will be thrown.
    /// </summary>
    public static readonly ExpectedVersion StreamDoesNotExist = new(0);

    /// <summary>
    /// The stream must exist. If it does not a <see cref="StreamDoesNotExistException"/> exception will be thrown.
    /// </summary>
    public static readonly ExpectedVersion StreamExists = new(-1);

    /// <summary>
    /// The stream state does not matter. If it exists it will be appended to, if it doesn't it will be created and
    /// appended to.
    /// </summary>
    public static readonly ExpectedVersion Any = new(-2);

    /// <summary>
    /// Create an expected version for a specific value.
    /// </summary>
    /// <param name="value">The expected version</param>
    /// <returns></returns>
    public static ExpectedVersion For(long value) => new(value);
}
