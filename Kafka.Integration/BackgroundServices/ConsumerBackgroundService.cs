using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Kafka.Integration.Core;
using Kafka.Integration.Core.Messages;
using Kafka.Integration.Core.Messages.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kafka.Integration.BackgroundServices;

public class ConsumerBackgroundWorker : BackgroundService
{
    private readonly string _topicName;
    private readonly IMessageTypeReader _messageTypeReader;
    private readonly ILogger<ConsumerBackgroundWorker> _log;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConsumer _consumer;
    private const int CommitPeriod = 5;

    public ConsumerBackgroundWorker(string topicName, IConsumer consumer, IMessageTypeReader messageTypeReader,
        ILogger<ConsumerBackgroundWorker> log, IServiceScopeFactory serviceScopeFactory)
    {
        _topicName = topicName;
        _messageTypeReader = messageTypeReader;


        _log = log;
        _serviceScopeFactory = serviceScopeFactory;
        _consumer = consumer;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Consumer.Subscribe(_topicName);

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.Consumer.Unsubscribe();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult =
                    _consumer.Consumer.Consume(stoppingToken);
                if (consumeResult is null)
                {
                    _log.LogInformation("consumeResult result is null");
                    continue;
                }

                if (!string.Equals(consumeResult.Topic, _topicName))
                {
                    continue;
                }

                if (consumeResult.IsPartitionEOF)
                {
                    continue;
                }

                if (consumeResult.Message.Value == null)
                {
                    continue;
                }

                var messageTypeBytes = consumeResult.Message.Headers.GetLastBytes(Constants.MessageTypeHeader);
                if (messageTypeBytes is null || messageTypeBytes.Length == 0)
                {
                    //skip messages without message type but will be committed it
                    continue;
                }


                var messageType = Encoding.UTF8.GetString(messageTypeBytes);
                var type = _messageTypeReader.Read(messageType).MatchWith(pattern: (
                    None: () =>
                    {
                        _log.LogWarning("Message type {MessageType} is not registered ", messageType);
                        throw new ApplicationException($"Message type {messageType} is not registered");
                    },
                    Some: v => v
                ));

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    await using var ms = new MemoryStream(consumeResult.Message.Value);
                    var message = await JsonSerializer.DeserializeAsync(ms,
                        typeof(MessageContext<>).MakeGenericType(type), cancellationToken: stoppingToken);
                    if (message is null)
                    {
                        var exception =
                            new ApplicationException($"Can't deserialize message by type {messageType}");
                        _log.LogError(exception.Message, exception);
                        throw exception;
                    }

                    var handlerType = typeof(IMessageHandler<>).MakeGenericType(type);
                    dynamic handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    //Generic workaround - https://stackoverflow.com/a/55716851
                    await handler.Handle((dynamic)message);
                }

                _log.LogInformation(consumeResult.Message.Key + " - " + type.Name);
                Commit(consumeResult);
            }
            catch (ConsumeException e)
            {
                _log.LogError("Consume error: {Reason}", e.Error.Reason);
            }
        }
    }

    private void Commit(ConsumeResult<long, byte[]> consumeResult)
    {
        if (consumeResult.Offset % CommitPeriod == 0)
        {
            // The Commit method sends a "commit offsets" request to the Kafka
            // cluster and synchronously waits for the response. This is very
            // slow compared to the rate at which the consumer is capable of
            // consuming messages. A high performance application will typically
            // commit offsets relatively infrequently and be designed handle
            // duplicate messages in the event of failure.
            try
            {
                _consumer.Consumer.Commit(consumeResult);
                _consumer.Consumer.StoreOffset(consumeResult);
            }
            catch (KafkaException e)
            {
                Console.WriteLine($"Commit error: {e.Error.Reason}");
            }
        }
    }
}