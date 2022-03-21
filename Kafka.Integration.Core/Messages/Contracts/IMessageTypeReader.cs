namespace Kafka.Integration.Core.Messages.Contracts;

public interface IMessageTypeReader
{
    Maybe<Type> Read(string name);
}

