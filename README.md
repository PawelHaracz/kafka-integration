# Kafka Integration

## Configuration

Application use normal Kafka configuration, everything has to be configured in `Kafka` section. 
Most important configuration is:
- `Kafka:BootstrapServers`
- `Kafka:BrokerAddressFamily`
- `Kafka:SecurityProtocol`
- `Kafka:SaslMechanism`
- `Kafka:SaslUsername`
- `Kafka:SaslPassword`
- `Kafka:GroupId`


```c#
  var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Kafka:BootstrapServers", "{EH-Name}.servicebus.windows.net:9093"},
                {"Kafka:BrokerAddressFamily", "V4"},
                {"Kafka:SecurityProtocol", "SaslSsl"},
                {"Kafka:SaslMechanism", "PLAIN"},
                {"Kafka:SaslUsername", "$ConnectionString"},
                {"Kafka:SaslPassword", "Endpoint=sb://{EH-Name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={AccessToken}"},
                {"Kafka:GroupId", "$Default"},
            });
        })
```
## Producer 

On the startup class please use extension method called  `AddEventBus<T>("Topic-Name")`. This method register Type of method to the selected kafka topic

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddEventBus<TestCommand>("test-topic-command");
    services.AddEventBus<TestCommand2>("test-topic-command");
    services.AddEventBus<TestEvent>("test-topic-event");
    services.AddEventBus<TestEvent2>("test-topic-event");
}
```

### Usage
Publishing a new message to the Kafka, it is needed to use `IEventBus` and invoke method `Public(Type)`
```c#
 using (var scope = host.Services.CreateScope())
{
    var busCommandDispatcher = scope.ServiceProvider.GetRequiredService<IEventBus>();
    await busCommandDispatcher.Public(new TestCommand(index));
}
```

## Consumer

Register the extension method `AddConsumerHandler<T, IMessageHandler<T>(Topic-Name)` that will create consumer to handler messages from the selected topic and send by type to the proper message handler

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddConsumerHandler<TestEvent, TestEventConsumer>("test-topic-event");
    services.AddConsumerHandler<TestEvent2, TestEvent2Consumer>("test-topic-event");
    services.AddConsumerHandler<TestCommand2, TestCommand2Consumer>("test-topic-command");
    services.AddConsumerHandler<TestCommand, TestCommandConsumer>("test-topic-command");
}
```

### Usage
Code needs to have implemented IMessageHandler by type of the message type. Handler will have received the MessageContext with proper type.

```c#
public class TestCommandConsumer : IMessageHandler<TestCommand>
{
    private readonly ILogger<TestCommandConsumer> _logger;
    
    public async Task Handle(IMessageContext<TestCommand> context)
    {
        _logger.LogInformation($"{DateTime.UtcNow} ~~ Received test Command: {context.Message.Index} - {context.Message.CreatedOn} - {context.MessageId} - {context.SentTime}");
    }
}
```