using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ChatService;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddYamlFile("config/appsettings.yaml", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        return WebHost.CreateDefaultBuilder(args)
            .UseConfiguration(config)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddLog4Net("config/log4net.xml");
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<RabbitMqSettings>(
                context.Configuration.GetSection("RabbitMQ"));
            })
            .UseStartup<Startup>();
    }
}