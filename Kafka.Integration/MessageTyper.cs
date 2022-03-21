using System.Collections.Concurrent;
using Kafka.Integration.Core;
using Kafka.Integration.Core.Messages.Contracts;

namespace Kafka.Integration;

public class MessageTyper :  IMessageTypeReader
{
    private MessageTyper(){}

    public static readonly Lazy<MessageTyper> Instance = new(new MessageTyper());

    private readonly ConcurrentDictionary<string, Type> _concurrentDictionary = new();

    public void Register<T>()
    {
        var type = typeof(T);
        _ = _concurrentDictionary.TryAdd(type.Name, type);
    }

    public Maybe<Type> Read(string name)
    {
        if (_concurrentDictionary.TryGetValue(name, out var type))
        {
            return new Some<Type>(type);
        }

        return new None<Type>();
    }
}
