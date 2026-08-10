using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using System;
using System.Text;

namespace AgentPortalApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args)
    {

        return WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddLog4Net("config/log4net.xml");
            })
            .Configure(a =>
            {
                var appSettings = new AppSettings();
                a.ApplicationServices.GetService<IConfiguration>()
                    .GetSection("AppSettings")
                    .Bind(appSettings);

                a.UseCors
                (b => b
                    .WithOrigins(appSettings.AllowedChatOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                );
                a.UseOcelot().Wait();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddYamlFile("config/appsettings.yaml", optional: false, reloadOnChange: true)
                    .AddYamlFile($"config/appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.yaml", optional: true, reloadOnChange: true)
                    .AddJsonFile($"config/ocelot.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, s) =>
            {
                var secret = context.Configuration["AppSettings:Secret"];
                var key = Encoding.ASCII.GetBytes(secret);

                s.AddCors();
                s.AddAuthentication(x =>
                    {
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer("ApiSecurity", x =>
                    {
                        var appSettings = new AppSettings();
                        var key = Encoding.ASCII.GetBytes(appSettings.Secret);
                        x.RequireHttpsMetadata = false;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                    });
                s.AddOcelot().AddEureka().AddCacheManager(x => x.WithDictionaryHandle());
            })
            .Build();
    }
}