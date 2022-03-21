using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.Integration.Core;

public interface IEventBus
{
    Task Public<T>(T data, CancellationToken token = default) where T : class;
}

public interface IEventBusReceiver
{
    IAsyncEnumerable<MessageType> Read(CancellationToken token = default);
}

public readonly struct MessageType : IEquatable<MessageType>
{
    public MessageType(MemoryStream content, string? contentType, string? topicName)
    {
        Content = content ?? Stream.Null;
        ContentType = contentType ?? string.Empty;
        TopicName = topicName ?? string.Empty;
    }

    public Stream Content { get; init; }
    public string ContentType { get; init; }
    public string TopicName { get; init; }

    public override int GetHashCode()
        => HashCode.Combine(Content, ContentType, TopicName);

    public override bool Equals(object? obj)
        => (obj is MessageType messageType) && Equals(messageType);

    public bool Equals(MessageType other)
        => (Content, ContentType, TopicName) == (other.Content, other.ContentType, TopicName);

    public static bool operator ==(MessageType left, MessageType right) =>
        Equals(left, right);

    public static bool operator !=(MessageType left, MessageType right) =>
        !Equals(left, right);
}
