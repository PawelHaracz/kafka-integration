namespace Kafka.Integration.Core.Messages.Contracts;

/// <summary>
/// Use for handling message when it is consuming by topic consumer
/// </summary>
/// <typeparam name="TMessage">Message type</typeparam>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// Logic to execute when message is handling
    /// </summary>
    /// <param name="context">Message context</param>
    /// <returns>Task</returns>
    Task Handle(IMessageContext<TMessage> context);
}