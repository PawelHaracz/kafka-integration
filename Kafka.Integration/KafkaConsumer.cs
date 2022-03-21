using Confluent.Kafka;
using Kafka.Integration.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kafka.Integration
{
    public class KafkaConsumer : IConsumer, IDisposable
    {
        public IConsumer<long, byte[]> Consumer { get; }
        public ConsumerConfig Config { get; }
        private readonly ILogger<KafkaConsumer> _log;

        public KafkaConsumer(ILogger<KafkaConsumer> log, IOptions<ConsumerConfig> options)
        {
            _log = log;
            Config = options.Value;
            Config.IsolationLevel = IsolationLevel.ReadUncommitted;
            Config.AutoOffsetReset = AutoOffsetReset.Earliest;
            Config.MaxPollIntervalMs = 300000;
            Config.EnableAutoOffsetStore = false;
            Config.EnableAutoCommit = false;

            //_config.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;
            Consumer = new ConsumerBuilder<long, byte[]>(Config)
                .SetKeyDeserializer(Deserializers.Int64)
                .SetValueDeserializer(Deserializers.ByteArray)
                .SetStatisticsHandler((consumer, s) =>
                {
                    _log.LogInformation(s);
                })
                .SetLogHandler((consumer, message) =>
                {
                    _log.LogInformation(message?.Message);
                })
                .SetErrorHandler((consumer, e) =>
                {
                    _log.LogError("Error: {error}, code: {code}", e.Reason, e.Code);
                })
                .Build();
        }

        public void Dispose()
        {
            Consumer.Close();
            Consumer.Dispose();
        }
    }
}
