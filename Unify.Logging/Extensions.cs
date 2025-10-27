using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.File.Archive;
using ILogger = Serilog.ILogger;

namespace Unify.Logging;

public static class Extensions
{
    public static ILogger AsEventLogger(this ILogger log, string eventType)
    {
        return log.ForContext("event-type", eventType);
    }
    
    public static ILoggerFactory AddUnifyLogging(this IHostBuilder builder, 
        [Optional] string[]? eventLoggers,
        [Optional] Assembly? assembly,
        [Optional] bool writeToConsole,
        [Optional] bool useAzureInsights)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }
        
        builder.ConfigureServices((ctx, services) =>
        {
            services.AddSingleton(new UnifyLogger());

            if (!useAzureInsights) return;
            
            var prop = ctx.Configuration["AzureMonitor:ConnectionString"];
            ArgumentNullException.ThrowIfNull(prop, "AzureMonitor:ConnectionString");
                
            services.AddOpenTelemetry()
                .WithLogging()
                .UseAzureMonitor();
        });

        LoggerConfiguration? config = null!;

        builder.ConfigureAppConfiguration((context, _) =>
        {
            if (eventLoggers != null && eventLoggers.Length != 0)
            {
                var eventConfig = AddEventLoggers(eventLoggers, context.Configuration,
                    context.HostingEnvironment.EnvironmentName, writeToConsole);
                Log.Logger = eventConfig.CreateLogger();
            }

            config = AddApplicationLog(assembly, context.Configuration, context.HostingEnvironment.EnvironmentName,
                writeToConsole);
        });

        if (config == null)
        {
            builder.UseSerilog((ctx, _) =>
            {
                config = AddApplicationLog(assembly, ctx.Configuration, ctx.HostingEnvironment.EnvironmentName,
                    writeToConsole);
            });
        }

        var logger = config!.CreateLogger();
        builder.UseSerilog(logger);

        var factory = LoggerFactory.Create(logging => { logging.AddSerilog(logger); });

        return factory;
    }

    public static WebApplication UseUnifyLogging(this WebApplication app, bool logRequests = false)
    {
        if (logRequests)
        {
            app.UseSerilogRequestLogging();
        }

        return app;
    }
    
    private static LoggerConfiguration AddEventLoggers(IEnumerable<string> eventLoggers, IConfiguration configuration,
        string environmentName, bool writeToConsole)
    {
        var outputTemplate = configuration["Unify:Logging:OutputTemplate"] 
                             ?? "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";
        
        var eventConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        foreach (var item in eventLoggers)
        {
            eventConfig.WriteTo.Logger(a =>
            {
                a.Enrich.WithProperty("event-type", item);
                a.Filter.ByIncludingOnly(x => x.Properties.ContainsKey("event-type") && x.Properties["event-type"].ToString() == $"\"{item}\"");
                a.WriteTo.Async(c => c.File($"App_Data/Logs/{item}_.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: outputTemplate,
                    hooks: new ArchiveHooks(CompressionLevel.Fastest, $"App_Data/Logs/_Archive/{item}")));
            });
        }

        if (environmentName == "Development" || writeToConsole)
        {
            eventConfig.WriteTo.Console();
        }

        return eventConfig;
    }

    private static LoggerConfiguration AddApplicationLog(Assembly assembly, IConfiguration configuration,
        string environmentName, bool writeToConsole)
    {
        var outputTemplate = configuration["Unify:Logging:OutputTemplate"] 
                             ?? "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";
        
        var config = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Async(a => a.File($"App_Data/Logs/{assembly.GetName().Name}_.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: outputTemplate,
                hooks: new ArchiveHooks(CompressionLevel.Fastest, "App_Data/Logs/_Archive")));

        if (environmentName == "Development" || writeToConsole)
        {
            config.WriteTo.Console();
        }

        return config;
    }
}