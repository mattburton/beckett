using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Beckett.Subscriptions;

public static class ChannelReaderExtensions
{
    public static IAsyncEnumerable<T[]> BatchReadAllAsync<T>(
        this ChannelReader<T> source,
        int batchSize,
        TimeSpan timeout
    )
    {
        return ReadBatches();

        async IAsyncEnumerable<T[]> ReadBatches(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                List<T> buffer = [];

                while (true)
                {
                    var token = buffer.Count == 0 ? cancellationToken : timer.Token;

                    var item = default(T);

                    try
                    {
                        item = await source.ReadAsync(token);
                    }
                    catch (ChannelClosedException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    if (buffer.Count == 0)
                    {
                        timer.CancelAfter(timeout);
                    }

                    if (item != null)
                    {
                        buffer.Add(item);

                        if (buffer.Count < batchSize)
                        {
                            continue;
                        }
                    }

                    yield return buffer.ToArray();

                    buffer.Clear();

                    if (timer.TryReset())
                    {
                        continue;
                    }

                    timer.Dispose();

                    timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                }

                if (buffer.Count > 0)
                {
                    yield return buffer.ToArray();
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (source.Completion.IsCompleted)
                {
                    await source.Completion;
                }
            }
            finally
            {
                timer.Dispose();
            }
        }
    }
}
