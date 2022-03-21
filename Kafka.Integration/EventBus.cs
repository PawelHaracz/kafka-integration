using System.Text.Json;
using System.Threading.Channels;
using Kafka.Integration.Core;
using Kafka.Integration.Core.Messages;
using Kafka.Integration.Core.Messages.Contracts;

namespace Kafka.Integration;

public class EventBus :IEventBus, IEventBusReceiver
{
    private readonly Channel<MessageType> _channel;
    private readonly IMessageTopicMapperFactory _messageTopicMapperFactory;

    public EventBus(Channel<MessageType> channel, IMessageTopicMapperFactory messageTopicMapperFactory)
    {
        _channel = channel;
        _messageTopicMapperFactory = messageTopicMapperFactory;
    }

    public async Task Public<T>(T data, CancellationToken token = default) where T : class
    {
        var message = new MessageContext<T>(Guid.NewGuid(), DateTime.UtcNow, data);
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        var topicNames = _messageTopicMapperFactory.TakeTopics<T>()
            .MatchWith((
            None: () => throw new ApplicationException($"type: {typeof(T).Name} doesn't have registered any topic"),
            Some: v => v
        ));
        var ms = new MemoryStream(json);
        var valueTasks = topicNames.Select(
            topicName => _channel.Writer.WriteAsync(new MessageType(ms, typeof(T).Name, topicName), token))
            .Select(vt => vt.AsTask());

        await Task.WhenAll(valueTasks);
    }

    public IAsyncEnumerable<MessageType> Read(CancellationToken token = default)
    {
        return _channel.Reader.ReadAllAsync(token);
    }
}

