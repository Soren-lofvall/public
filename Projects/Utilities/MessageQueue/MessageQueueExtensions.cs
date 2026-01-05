using Microsoft.Extensions.DependencyInjection;
using System;

namespace Utilities.MessageQueue;

public static class MessageQueueExtensions
{
    public static IServiceCollection AddMessageQueue<T>(this IServiceCollection services, int queueSize) where T : IMessage
    {
        services.AddSingleton<IMessageQueueRegistry<T>, MessageQueueRegistry<T>>();
        services.AddSingleton<IMessageQueue<T>>(
            _ => new BoundedQueue<T>(queueSize));
        services.AddHostedService<MessageQueueWorker<T>>();

        return services;
    }

    public static IMessageQueue<T> GetMessageQueue<T>(this IServiceProvider services) where T : IMessage
    {
        return services.GetRequiredService<IMessageQueue<T>>();
    }

    public static IMessageQueueRegistry<T> GetMessageQueueRegistry<T>(this IServiceProvider services) where T : IMessage
    {
        return services.GetRequiredService<IMessageQueueRegistry<T>>();
    }
}
