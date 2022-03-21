namespace Kafka.Integration.Core;

/// <summary>
/// Use to registry consumers
/// </summary>
public interface ITopicMessageConsumersRegistration
{
    /// <summary>
    /// Registry consumers for topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="configure">Consumer handlers registration</param>
    /// <returns></returns>
    ITopicMessageConsumersRegistration AddTopicConsumers(string topicName, Action<string, ITopicConsumerRegistration> configure);
}

