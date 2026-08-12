using System;
using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using PolicyService.Messaging.RabbitMq.Outbox;

namespace PolicyService.Messaging.RabbitMq;

public static class RabbitInstaller
{
    public static IServiceCollection AddRabbitListeners(this IServiceCollection services, RabbitMqSettings options)
    {
        var connectionString = options.ConnectionString;
        var bus = RabbitHutch.CreateBus(connectionString);
        bus.Advanced.ExchangeDeclare("lab-dotnet-micro", ExchangeType.Topic);
        services.AddSingleton(bus);
        
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<Outbox.Outbox>();
        services.AddHostedService<OutboxSendingService>();
        return services;
    }
}