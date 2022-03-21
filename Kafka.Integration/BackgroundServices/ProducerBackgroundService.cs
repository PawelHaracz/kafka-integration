using System.Text;
using Confluent.Kafka;
using Kafka.Integration.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kafka.Integration.BackgroundServices;

public class ProducerBackgroundService : BackgroundService
{
    private readonly string _topicName;
    private readonly ILogger<ProducerBackgroundService> _log;
    private readonly IProducer _producer;
    private readonly IEventBusReceiver _eventBusReceiver;
    public ProducerBackgroundService(string topicName, IProducer producer, IEventBusReceiver eventBusReceiver, ILogger<ProducerBackgroundService> log)
    {
        _topicName = topicName;
        _log = log;
        _producer = producer;
        _eventBusReceiver = eventBusReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await foreach(var messageType in _eventBusReceiver.Read(stoppingToken))
        {
            if (!string.Equals(messageType.TopicName, _topicName))
            {
                continue;
            }
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            await using var ms = new MemoryStream();
            await messageType.Content.CopyToAsync(ms, stoppingToken);
            var bytes = ms.ToArray();

            var message = new Message<long, byte[]>()
            {
                Key = DateTime.UtcNow.Ticks,
                Value = bytes,
                Headers = new Headers()
                {
                    new Header(Constants.MessageTypeHeader, Encoding.UTF8.GetBytes(messageType.ContentType))
                }
            };

            try
            {
                _producer.Producer.Produce(_topicName, message,   (deliveryReport) =>
                {
                    if (deliveryReport.Status != PersistenceStatus.Persisted)
                    {
                        // delivery might have failed after retries. This message requires manual processing.
                        _log.LogError(
                            $"ERROR: Message not ack'd by all brokers (value: '{message}'). Delivery status: {deliveryReport.Status}");
                    }
                });

                var queueSize =  _producer.Producer.Flush(TimeSpan.FromSeconds(5));
                if (queueSize > 0)
                {
                    _log.LogInformation("WARNING: Producer event queue has " + queueSize + " pending events on exit.");
                }
            }
            catch (ProduceException<long, byte[]> e)
            {
                _log.LogError($"failed to deliver message: {e.Message} [{e.Error.Code}]");
            }
        }
    }
}