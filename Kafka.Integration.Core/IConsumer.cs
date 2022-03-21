using Confluent.Kafka;
namespace Kafka.Integration.Core;

    public interface IConsumer
    {
        IConsumer<long, byte[]> Consumer { get; }
        ConsumerConfig Config { get; }
    }

