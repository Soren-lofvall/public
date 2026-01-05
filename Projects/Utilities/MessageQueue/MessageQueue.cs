
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Utilities.MessageQueue;

public interface IMessage { }

public interface IMessageQueue<TMessage>
{
    ValueTask EnqueueAsync(TMessage message, CancellationToken ct = default);
    ValueTask<TMessage> DequeueAsync(CancellationToken ct = default);
}

public interface IMessageQueueTarget<TMessage>
    where TMessage : IMessage
{
    ValueTask PublishAsync(TMessage message, CancellationToken ct);
}

public sealed class MessageQueueTarget<TMessage>(IMessageQueue<TMessage> queue) : IMessageQueueTarget<TMessage>
    where TMessage : IMessage
{
    public ValueTask PublishAsync(TMessage message, CancellationToken ct)
        => queue.EnqueueAsync(message, ct);
}

public sealed class MessageQueueCallbackTarget<TMessage>(
    Func<TMessage, CancellationToken, ValueTask> handler) : IMessageQueueTarget<TMessage>
    where TMessage : IMessage
{
    public ValueTask PublishAsync(TMessage message, CancellationToken ct)
        => handler(message, ct);
}

public interface IMessageQueueRegistry<TMessage>
    where TMessage : IMessage
{
    IReadOnlyCollection<IMessageQueueTarget<TMessage>> Targets { get; }

    void AddTarget(IMessageQueueTarget<TMessage> target);
}

public class MessageQueueRegistry<TMessage> : IMessageQueueRegistry<TMessage>
    where TMessage : IMessage
{
    private readonly ConcurrentBag<IMessageQueueTarget<TMessage>> _targets = [];

    public IReadOnlyCollection<IMessageQueueTarget<TMessage>> Targets => _targets;

    public void AddTarget(IMessageQueueTarget<TMessage> target)
        => _targets.Add(target);
}

public static class MessageQueueRegistration
{
    public static BoundedQueue<TMessage> NewQueueConsumer<TMessage>(
        this IMessageQueueRegistry<TMessage> registry,
        int queueSize)
        where TMessage : IMessage
    {
        var consumerQueue = new BoundedQueue<TMessage>(queueSize);
        registry.AddTarget(new MessageQueueTarget<TMessage>(consumerQueue));
        return consumerQueue;
    }

    public static void AddQueueConsumer<TMessage>(
        this IMessageQueueRegistry<TMessage> registry,
        IMessageQueue<TMessage> queue)
        where TMessage : IMessage
    {
        registry.AddTarget(new MessageQueueTarget<TMessage>(queue));
    }

    public static void AddCallbackConsumer<TMessage>(
        this IMessageQueueRegistry<TMessage> registry,
        Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : IMessage
    {
        registry.AddTarget(new MessageQueueCallbackTarget<TMessage>(handler));
    }
}


public class MessageQueueWorker<TMessage>(
    IMessageQueue<TMessage> input,
    IMessageQueueRegistry<TMessage> registry) : BackgroundService
    where TMessage : IMessage
{
    private readonly IMessageQueue<TMessage> _input = input;
    private readonly IMessageQueueRegistry<TMessage> _registry = registry;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var message = await _input.DequeueAsync(ct);

            foreach (var target in _registry.Targets)
            {
                await target.PublishAsync(message, ct);
            }
        }
    }
}

