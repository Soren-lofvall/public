using System.Threading.Channels;

namespace Utilities.MessageQueue;

public class BoundedQueue<T> : IMessageQueue<T>
{
    private readonly Channel<T> _channel;

    public BoundedQueue(int capacity)
    {
        _channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public ValueTask EnqueueAsync(T message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public ValueTask<T> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);
}
