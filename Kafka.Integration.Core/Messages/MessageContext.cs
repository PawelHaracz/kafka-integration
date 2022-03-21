using Kafka.Integration.Core.Messages.Contracts;
namespace Kafka.Integration.Core.Messages;

public sealed class MessageContext<TMessage> : IMessageContext<TMessage> where TMessage : class
{
    public Guid MessageId { get; }
    public DateTime SentTime { get; }
    public TMessage Message { get; }

    public MessageContext(Guid messageId, DateTime sentTime, TMessage message)
    {
        MessageId = messageId;
        SentTime = sentTime;
        Message = message;
    }
}
