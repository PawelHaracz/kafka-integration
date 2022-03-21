using System.Collections.Concurrent;
using Kafka.Integration.Core;
using Kafka.Integration.Core.Messages.Contracts;

namespace Kafka.Integration;

public class TopicMessageMapper :  IMessageTopicMapperFactory, IMessageTopicMapperWriter
{
    private TopicMessageMapper(){}

    public static readonly Lazy<TopicMessageMapper> Instance = new(new TopicMessageMapper());

    private readonly ConcurrentBag<TopicTypeValue> _concurrentBag = new();
    public void Register<TMessage>(string topicName) where TMessage : class
    {
        _concurrentBag.Add(new TopicTypeValue(topicName, typeof(TMessage).Name));
    }

    public Maybe<IEnumerable<string>> TakeTopics<T>()
    {
        var array = _concurrentBag.ToArray();
        var topicValue =  array
            .Where(value => value.TypeName == typeof(T).Name)
            .Select(v => v.TopicName)
            .ToArray();

        if (!topicValue.Any())
        {
           return new None<IEnumerable<string>>();
        }

        return new Some<IEnumerable<string>>(topicValue);
    }


    public Maybe<IEnumerable<string>> TakeTypes(string topicName)
    {
        var array = _concurrentBag.ToArray();
        var topicValue =  array
            .Where(value => string.Equals(topicName, value.TopicName) )
            .Select(v => v.TypeName)
            .Distinct()
            .ToArray();

        if (!topicValue.Any())
        {
            return new None<IEnumerable<string>>();
        }

        return new Some<IEnumerable<string>>(topicValue);
    }
    private readonly struct TopicTypeValue : IEquatable<TopicTypeValue>, IComparable<TopicTypeValue>
    {
        public static TopicTypeValue Default => new(string.Empty, string.Empty);
        public string TopicName { get; init; }
        public string TypeName { get; init;  }

        public TopicTypeValue(string topicName, string typeName)
        {
            TopicName = topicName;
            TypeName = typeName;
        }

        public override int GetHashCode()
            => HashCode.Combine(TypeName, TopicName);

        public override bool Equals(object? obj)
            => (obj is TopicTypeValue messageType) && Equals(messageType);

        public bool Equals(TopicTypeValue other)
            => (TypeName, TopicName) == (other.TypeName, other.TopicName);

        public static bool operator ==(TopicTypeValue left, TopicTypeValue right) =>
            Equals(left, right);

        public static bool operator !=(TopicTypeValue left, TopicTypeValue right) =>
            !Equals(left, right);

        public int CompareTo(TopicTypeValue other)
        {
            var topicNameComparison = string.Compare(TopicName, other.TopicName, StringComparison.Ordinal);
            if (topicNameComparison != 0) return topicNameComparison;
            return string.Compare(TypeName, other.TypeName, StringComparison.Ordinal);
        }
    }
}
