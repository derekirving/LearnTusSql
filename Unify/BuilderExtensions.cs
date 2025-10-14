#if NET
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.Logging;
using Unify.Configuration;

namespace Unify;

public static class BuilderExtensions
{
    // For Web Apps
    public static WebApplicationBuilder AddUnifyConfiguration(this WebApplicationBuilder builder, ILogger? logger = null)
    {
        BuildConfig(builder.Host, logger);
        return builder;
    }
    
    public static T AddUnifyConfiguration<T>(this WebApplicationBuilder builder, ILogger? logger = null) where T : UnifyBaseConfiguration, new()
    {
        return BuildConfig<T>(builder.Host, logger);
    }
    // For Command line interfaces
    
    public static IHostBuilder AddUnifyConfiguration(this IHostBuilder builder, ILogger? logger = null)
    {
        BuildConfig(builder, logger);
        return builder;
    }
    
    public static IHostBuilder AddUnifyConfiguration(this IHostBuilder builder, string userSecretsId, ILogger? logger = null)
    {
        BuildConfig(builder, userSecretsId, logger);
        return builder;
    }
    
    public static T AddUnifyConfiguration<T>(this IHostBuilder builder, ILogger? logger = null) where T : UnifyBaseConfiguration, new()
    {
        return BuildConfig<T>(builder, logger);
    }
    
    public static T AddUnifyConfiguration<T>(this IHostBuilder builder, string userSecretsId,  ILogger? logger = null) where T : UnifyBaseConfiguration, new()
    {
        return BuildConfig<T>(builder, logger, userSecretsId);
    }

    private static void BuildConfig(IHostBuilder builder, string userSecretsId, ILogger? logger)
    {
        builder.ConfigureAppConfiguration((context, config) => config.AddUnifyConfiguration(context, userSecretsId, logger));
    }

    private static void BuildConfig(IHostBuilder builder, ILogger? logger)
    {
        builder.ConfigureAppConfiguration((context, config) => config.AddUnifyConfiguration(context, logger));
    }
    
    private static T BuildConfig<T>(IHostBuilder builder, ILogger? logger, string userSecretsId = "") where T : UnifyBaseConfiguration, new()
    {
        T options = new();
        
        builder.ConfigureAppConfiguration((context, config) => config.AddUnifyConfiguration(context, logger, userSecretsId));
        builder.ConfigureServices((context, services) =>
        {
            options = services.AddApplicationOptions<T>(context.Configuration);
            services.AddSingleton<IPostConfigureOptions<T>, ConfigureSettingsOptions<T>>();
        });

        options.BasePath = Environment.CurrentDirectory;
        return options;
    }
}
#endif