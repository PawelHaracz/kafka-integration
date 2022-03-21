namespace Kafka.Integration.Core.Messages.Contracts;

/// <summary>
/// Message context
/// </summary>
/// <typeparam name="TMessage">Message type</typeparam>
public interface IMessageContext<out TMessage> where TMessage : class
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    public Guid MessageId { get; }
    /// <summary>
    /// Time when message was sent
    /// </summary>
    public DateTime SentTime { get; }
    /// <summary>
    /// Message that was sent
    /// </summary>
    public TMessage Message { get; }
}