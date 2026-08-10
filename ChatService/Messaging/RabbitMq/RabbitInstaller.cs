using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace ChatService.Messaging.RabbitMq;

public static class RabbitInstaller
{
    public static IServiceCollection AddRabbitListeners(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitSettings = new RabbitMqSettings();
        configuration.GetSection("RabbitMQ").Bind(rabbitSettings);
        var connectionString = rabbitSettings.ConnectionString;
        var bus = RabbitHutch.CreateBus(connectionString);
        bus.Advanced.ExchangeDeclare("lab-dotnet-micro", ExchangeType.Topic);
        services.AddSingleton(bus);
        
        services.AddSingleton(svc => new RabbitEventListener(svc.GetRequiredService<IBus>(), svc));

        return services;
    }
}

public static class RabbitListenersInstaller
{
    public static void UseRabbitListeners(this IApplicationBuilder app, List<Type> eventTypes)
    {
        app.ApplicationServices.GetRequiredService<RabbitEventListener>().ListenTo(eventTypes);
    }
}