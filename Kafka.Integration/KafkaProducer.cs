using Confluent.Kafka;
using Kafka.Integration.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kafka.Integration;

public class KafkaProducer: IProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _log;
    private readonly ProducerConfig _config;
    public IProducer<long, byte[]> Producer { get; }

    public KafkaProducer(ILogger<KafkaProducer> log, IOptions<ProducerConfig> options)
    {
        _log = log;
        _config = options.Value;
        _config.MessageSendMaxRetries = 3;
        _config.EnableIdempotence = true;
        _config.RetryBackoffMs = 1000;
        _config.Acks = Acks.All;
        _config.EnableDeliveryReports = true;

        Producer = new ProducerBuilder<long, byte[]>(_config)
            .SetKeySerializer(Serializers.Int64)
            .SetValueSerializer(Serializers.ByteArray)
            .SetStatisticsHandler((producer, s) =>
            {
                _log.LogTrace(s);
            })
            .SetLogHandler((producer, message) =>
            {
                _log.LogInformation(message?.Message);
            })
            .SetErrorHandler((producer, error) =>
            {
                _log.LogError("Error: {error}, code: {code}", error.Reason, error.Code);
            })
            .Build();
    }

    public void Dispose() => Producer.Dispose();
}