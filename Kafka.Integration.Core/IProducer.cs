using Confluent.Kafka;

namespace Kafka.Integration.Core;

public interface IProducer
{
    IProducer<long, byte[]> Producer { get; }
}