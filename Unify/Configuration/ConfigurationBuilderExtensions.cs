#if NET
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Unify.Models;

namespace Unify.Configuration;

public static class ConfigurationBuilderExtensions
{
    private static ILogger? Logger { get; set; }
    private static IConfigurationBuilder? Builder { get; set; }

    private static HostBuilderContext? HostBuilderContext { get; set; }
    private static string? RemotePath { get; set; }
    private static RetryPolicy? RetryPolicy { get; set; }
    private static Context? Context { get; set; }

    public static IConfigurationBuilder AddUnifyConfiguration(this IConfigurationBuilder builder,
        HostBuilderContext hostBuilderContext, string userSecretsId, ILogger? logger)
    {
        builder.AddUnifyConfiguration(hostBuilderContext, logger, userSecretsId);
        return builder;
    }

    public static IConfigurationBuilder AddUnifyConfiguration(this IConfigurationBuilder builder,
        HostBuilderContext hostBuilderContext, ILogger? logger, string userSecretsId = "")
    {
        Builder = builder;
        HostBuilderContext = hostBuilderContext;

        if (logger != null)
        {
            Logger = logger;
        }

        Logger?.LogInformation("Unify.Configuration > Starting");

        RemotePath = Environment.GetEnvironmentVariable(Constants.UnifyPathEnvironment)
                     ?? Environment.GetEnvironmentVariable(Constants.UnifyPathEnvironment,
                         EnvironmentVariableTarget.User);

        if (string.IsNullOrEmpty(RemotePath))
        {
            Logger?.LogCritical(
                "Unify.Configuration > Configuration failed: There is no {path} environment variable",
                Constants.UnifyPathEnvironment);

            throw new ArgumentException(
                $"Unify.Configuration > There is no {Constants.UnifyPathEnvironment} environment variable");
        }

        Context = new Context(nameof(AddUnifyConfiguration));

        if (Logger != null)
        {
            Context.AddLogger(Logger);
        }

        var delay = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5);

        RetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(delay,
                (exception, span, retryCount, ctx) =>
                {
                    Logger?.LogWarning(exception,
                        "Unify.Configuration > {key} Retrying ({count}) because {message} - {span}",
                        ctx.OperationKey, retryCount, exception.Message, span);
                });

        if (hostBuilderContext.HostingEnvironment.IsDevelopment())
        {
            LoadUserSecrets(userSecretsId);
        }

        if (hostBuilderContext.HostingEnvironment.IsProduction())
        {
            Execute(() => LoadProductionSecrets(userSecretsId));
        }

        Execute(LoadRequiredConfig);
        Execute(LoadRequestedConnectionStrings);
        
        // Calling this again with the 'true' overload allows for global secrets/connection string to be overwritten but only during development
        if (hostBuilderContext.HostingEnvironment.IsDevelopment())
        {
            LoadUserSecrets(userSecretsId, true);
        }

        // TODO: Make Obsolete as this now gets configured elsewhere more efficiently
        var settings = new Dictionary<string, string>
        {
            { "Unify:ConfigLoaded", DateTime.Now.ToString("f") }
        };

        builder.AddInMemoryCollection(settings!);

        return builder;
    }

    // TODO: Make Obsolete as can now be configured strongly typed
    public static string GetUnifyAppBaseUrl(this IConfiguration config)
    {
        return config[Constants.UnifyAppBaseUrl] ?? string.Empty;
    }

    public static string GetUnifyAppVirtualDirectory(this IConfiguration config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config[Constants.UnifyAppBaseUrl]);

        var uri = new Uri(config[Constants.UnifyAppBaseUrl]!);
        var path = uri.LocalPath.TrimEnd('/');
        return path;
    }
    
    private static void Execute(Action action)
    {
        var result = RetryPolicy!.ExecuteAndCapture(_ => action(), Context);

        if (result.Outcome != OutcomeType.Failure) return;

        var methodName = action.Method.Name;
        LogAndThrow(methodName, result.FinalException);
    }

    private static void LoadUserSecrets(string userSecretsId, bool toOverwrite = false)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string secretsPath;

        if (OperatingSystem.IsWindows())
        {
            secretsPath = Path.Combine(appData, Constants.MicrosoftFolder, Constants.UserSecretsFolder,
                userSecretsId, Constants.UnifySecretsFile);
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            secretsPath = Path.Combine(home, Constants.MicrosoftFolderUnix,
                Constants.UserSecretsFolder.ToLowerInvariant(), userSecretsId, Constants.UnifySecretsFile);
        }
        else
        {
            throw new PlatformNotSupportedException("The current operating system is not supported.");
        }

        if (File.Exists(secretsPath))
        {
            if (!toOverwrite)
            {
                Logger?.LogInformation(
                    "Unify.Configuration > Development: Loading local secrets from {SecretsPath}", secretsPath);
            }
            else
            {
                Logger?.LogInformation(
                    "Unify.Configuration > Development: Overwriting any global secrets from {SecretsPath}", secretsPath);
            }

            Builder!.AddJsonFile(secretsPath, true, false);
        }
        else
        {
            Logger?.LogWarning("Unify.Configuration > Development: User Secrets not found in {SecretsPath}",
                secretsPath);
        }
    }

    private static void LoadGlobalConfig()
    {
        var globalConfigPath = Path.Combine(RemotePath!, Constants.UnifyGlobalSecretsFile);
        Builder!.AddJsonFile(globalConfigPath, false, false);
        Logger?.LogWarning("Unify.Configuration > Adding ALL global config from {Path}. Consider using Unify:RequiredGlobalConfig", globalConfigPath);
    }

    private static void LoadRequiredConfig()
    {
        var requiredConfig =
            HostBuilderContext!.Configuration.GetSection("Unify:RequiredGlobalConfig").Get<List<string>>() ??
            [];

        if (requiredConfig.Count == 0)
        {
            LoadGlobalConfig();
            return;
        }

        var hardcodedDefaults = new List<string>
        {
            "theme:cdn"
        };

        requiredConfig.AddRange(hardcodedDefaults);

        var globalConfigPath = Path.Combine(RemotePath!, Constants.UnifyGlobalSecretsFile);

        using var jsonStream = File.OpenRead(globalConfigPath);
        var jsonDoc = JsonDocument.Parse(jsonStream);

        foreach (var configKey in requiredConfig.Select(item => $"unify:{item}"))
        {
            var jsonPathSegments = configKey.Split(':');

            var jsonElement = GetJsonElementByPath(jsonDoc.RootElement, jsonPathSegments);

            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var settings = new Dictionary<string, string>
                    {
                        { configKey, jsonElement.GetString()! }
                    };

                    Logger?.LogInformation("Unify.Configuration > Adding value {Item}", configKey);
                    Builder!.AddInMemoryCollection(settings!);
                    break;
                }
                case JsonValueKind.Object:
                {
                    foreach (var settings in from property in jsonElement.EnumerateObject()
                             let fullKey = $"{configKey}:{property.Name}"
                             let value = property.Value.ToString()
                             select new Dictionary<string, string>
                             {
                                 { fullKey, value }
                             })
                    {
                        
                        Builder!.AddInMemoryCollection(settings!);
                    }
                    Logger?.LogInformation("Unify.Configuration > Adding values {Item}", configKey + ":*");
                    break;
                }
                default:
                    throw new ArgumentException(
                        $"Unify Configuration > Failed to determine the JsonValueKind for {configKey}");
            }
        }
    }

    private static void LoadRequestedConnectionStrings()
    {
        var requiredConnectionStrings = HostBuilderContext!.Configuration
            .GetSection("Unify:RequiredConnectionStrings").GetChildren().ToArray();

        if (requiredConnectionStrings.Length == 0) return;

        var path = Path.Combine(RemotePath!, Constants.UnifyConnectionStringsFile);

        if (!File.Exists(path))
        {
            Logger?.LogWarning("Unify Configuration > Found \"RequiredConnectionStrings\" but didn't find {file}",
                path);
            throw new FileNotFoundException(path);
        }

        var json = File.ReadAllText(path);

        var result = JsonSerializer.Deserialize<RequiredConnectionStrings>(json);

        var dict = result?.ConnectionStrings;

        foreach (var item in requiredConnectionStrings)
        {
            var configuredString = HostBuilderContext.Configuration[$"ConnectionStrings:{item.Value}"];

            // Checking this value allows us to have the connection string configured in appsettings.Development.json
            // while still being able to specify the `RequiredConnectionString` in appsettings.json

            if (!string.IsNullOrWhiteSpace(configuredString)) continue;

            var cs = string.Empty;
            dict?.TryGetValue(item.Value!, out cs);
            if (!string.IsNullOrWhiteSpace(cs))
            {
                Logger?.LogInformation("Unify Configuration > Adding connection string \"{conn}\"", item.Value);
                HostBuilderContext.Configuration[$"ConnectionStrings:{item.Value}"] = cs;
            }
            else
            {
                throw new ArgumentException("Unify.Configuration > The requested connection string does not exist",
                    item.Value);
            }
        }
    }

    private static void LoadProductionSecrets(string? userSecretsId = "")
    {
        // Keep backwards compatibility

        if (string.IsNullOrEmpty(userSecretsId))
        {
            var currentDirectory = Environment.CurrentDirectory;
            var unifyAppIdFile = Path.Combine(currentDirectory, Constants.UnifyAppId);

            if (!File.Exists(unifyAppIdFile)) return;

            userSecretsId = File.ReadAllText(unifyAppIdFile);
        }

        var path = Path.Combine(RemotePath!, Constants.UnifySecretsFolder, userSecretsId,
            Constants.UnifySecretsFile);

        if (!File.Exists(path))
        {
            Logger?.LogWarning("Unify.Configuration > No secrets at {Path}", path);
            return;
        }

        Logger?.LogInformation("Unify.Configuration > Loading production secrets from {Path}", path);
        var dirInfo = new DirectoryInfo(path);
        // Not going to try to reload on change anymore as this may have caused the problem of existing configuration vanishing
        Builder!.AddJsonFile(dirInfo.FullName, true, false);
    }

    private static JsonElement GetJsonElementByPath(JsonElement element, string[] pathSegments)
    {
        foreach (var segment in pathSegments)
        {
            var found = false;

            foreach (var property in element.EnumerateObject().Where(property =>
                         string.Equals(property.Name, segment, StringComparison.CurrentCultureIgnoreCase)))
            {
                element = property.Value;
                found = true;
                break;
            }

            if (!found)
            {
                return default; // Return a default JsonElement if path does not exist
            }
        }

        return element;
    }

    private static void LogAndThrow(string methodName, Exception exception)
    {
        Logger?.LogCritical(exception, "Unify.Configuration > {Method} failed: {message}",
            methodName, exception.Message);

        throw exception;
    }
}
#endif