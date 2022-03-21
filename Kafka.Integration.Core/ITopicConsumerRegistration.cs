using Kafka.Integration.Core.Messages.Contracts;

namespace Kafka.Integration.Core;

/// <summary>
/// Use to registry handlers for topic message type
/// </summary>
public interface ITopicConsumerRegistration
{
    /// <summary>
    /// Registry handler for message type
    /// </summary>
    /// <typeparam name="TMessage">Message type</typeparam>
    /// <typeparam name="THandler">Handler type</typeparam>
    void AddConsumerHandler<TMessage, THandler>(string topicName) where TMessage : class where THandler : class, IMessageHandler<TMessage>;
}

