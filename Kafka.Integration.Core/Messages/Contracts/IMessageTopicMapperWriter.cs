namespace Kafka.Integration.Core.Messages.Contracts;

public interface IMessageTopicMapperWriter
{
    void Register<TMessage>(string topicName) where TMessage: class;
}
