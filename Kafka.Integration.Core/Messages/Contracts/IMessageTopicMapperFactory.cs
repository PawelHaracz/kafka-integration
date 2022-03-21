namespace Kafka.Integration.Core.Messages.Contracts;

public interface IMessageTopicMapperFactory
{
    Maybe<IEnumerable<string>> TakeTopics<T>();
    Maybe<IEnumerable<string>> TakeTypes(string topicName);
}
