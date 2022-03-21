using System.Threading.Channels;
using Confluent.Kafka;
using Kafka.Integration.BackgroundServices;
using Kafka.Integration.Core;
using Kafka.Integration.Core.Messages.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Kafka.Integration;

public static class MessageBrokerRegistration
{
    public static IServiceCollection AddEventBus<T>(this IServiceCollection services, string topicName) where T: class
    {
        TopicMessageMapper.Instance.Value.Register<T>(topicName);
        services.TryAddSingleton(TopicMessageMapper.Instance.Value);
        services.TryAddSingleton<IMessageTopicMapperFactory>(provider => provider.GetRequiredService<TopicMessageMapper>());
        services.TryAddSingleton<IMessageTopicMapperWriter>(provider => provider.GetRequiredService<TopicMessageMapper>());
        services.AddOptions<ProducerConfig>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                var kafkaConfig = configuration.GetSection("kafka");
                kafkaConfig.Bind(config);
            });
        services.TryAddSingleton(provider =>
            new EventBus(
                Channel.CreateUnbounded<MessageType>(
                    new UnboundedChannelOptions()
                    {
                        SingleReader = false,
                        SingleWriter = false,
                    }),
                provider.GetRequiredService<IMessageTopicMapperFactory>()));
        services.TryAddSingleton<IEventBus>(provider => provider.GetRequiredService<EventBus>());
        services.TryAddSingleton<IEventBusReceiver>(provider => provider.GetRequiredService<EventBus>());
        services.TryAddSingleton<IProducer, KafkaProducer>();

       var registeredTypes = TopicMessageMapper.Instance.Value.TakeTypes(topicName)
           .Map(enumerable => enumerable.Count()).MatchWith(pattern: (
            None: () => 0,
            Some: v => v
        ));

       if (registeredTypes == 1)
       {
           services.AddSingleton<IHostedService>(provider =>
               new ProducerBackgroundService(
                   topicName,
                   provider.GetRequiredService<IProducer>(),
                   provider.GetRequiredService<IEventBusReceiver>(),
                   provider.GetRequiredService<ILogger<ProducerBackgroundService>>()));
       }

       return services;

    }
    public static IServiceCollection AddConsumerHandler<TMessage, THandler>(this IServiceCollection services, string topicName)
        where TMessage : class where THandler : class, IMessageHandler<TMessage>
    {
        MessageTyper.Instance.Value.Register<TMessage>();
        TopicMessageMapper.Instance.Value.Register<TMessage>(topicName);
        services.TryAddSingleton(TopicMessageMapper.Instance);
        services.TryAddSingleton<IConsumer, KafkaConsumer>();
        services.TryAddSingleton<IMessageTypeReader>(_ => MessageTyper.Instance.Value);

        services.AddOptions<ConsumerConfig>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                var kafkaConfig = configuration.GetSection("kafka");
                kafkaConfig.Bind(config);
            });

        var registeredTypes = TopicMessageMapper.Instance.Value.TakeTypes(topicName)
            .Map(enumerable => enumerable.Count())
            .MatchWith((
            None: () => 0,
            Some: v => v
        ));

        if (registeredTypes == 1)
        {
            services.AddSingleton<IHostedService>(provider =>
                new ConsumerBackgroundWorker(topicName,
                    provider.GetRequiredService<IConsumer>(),
                    provider.GetRequiredService<IMessageTypeReader>(),
                    provider.GetRequiredService<ILogger<ConsumerBackgroundWorker>>(),
                    provider.GetRequiredService<IServiceScopeFactory>()
                ));
        }

        return services
            .AddSingleton<IMessageHandler<TMessage>, THandler>();
    }
}
