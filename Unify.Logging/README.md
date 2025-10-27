# Unify.Logging

Unify applications can use any logging framework but this package provides easy integration of [Serilog](https://serilog.net/).

The package supports both Web and Console/Cli apps. For each app type, configuration is the same and the logs are written to **./App_Data/logs**

They have a daily rolling interval, appending the time period to the filename, creating a file set like:

```bash
Application_20231025.log
Application_20231026.log
Application_20231027.log
```

To avoid bringing down apps with runaway disk usage the file sink limits file size to 1GB by default. Once the limit is reached, no further events will be written until the next roll point.

Completed log files are archived before they are deleted by Serilog's retention mechanism.

These archives are GZipped in to the directory `App_Data\Logs\_Archive`. **This directory is not monitored for size usage** - archived logs should be cleared out manually as required.

Here is an example that logs `Verbose` during debugging and `Information` during production and where all Unify log messages will be `Verbose` in both development and production.

appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Unify": "Verbose"
      }
    }
  }
}
```

appsettings.Development.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "System": "Information",
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    }
  }
}
```

## Output Template

To configure exactly what is logged, add an **OutputTemplate** in `Unify:Logging:OutputTemplate`:

```json
{
  "Unify": {
    "Logging": {
      "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
    }
  }
}
```

## Console/Cli App Example

```c#
using Cocona;
using Microsoft.Extensions.Configuration;
using Unify.Logging;
using ILogger = Serilog.ILogger;

var builder = CoconaApp.CreateBuilder();
builder.Host.AddUnifyLogging(writeToConsole: true);

var app = builder.Build();

app.AddCommand((IConfiguration config, ILogger logger) =>
{
    logger.Information("{App} is logging with level {Level} (from appsettings)", nameof(Program),
        config["Serilog:MinimumLevel:Default"]);
});

await app.RunAsync();
```

## WebApp Example

```c#
using Serilog;
using Unify.Logging;
using ILogger = Serilog.ILogger;

try
{
    var builder = WebApplication.CreateBuilder(args);
    using var factory = builder.Host.AddUnifyLogging();
    
    var app = builder.Build();

    app.UseUnifyLogging(logRequests: true);

    app.MapGet("/", (IConfiguration config, ILogger logger) =>
    {
        logger.Information("{App} is logging with level {Level} (from appsettings)", nameof(Program),
            config["Serilog:MinimumLevel:Default"]);

        return "Hello World!";
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
```

## Event Loggers

It's often useful to be able to create and pass loggers to static methods that will accept them:

```c#
using var factory = builder.Host.AddUnifyLogging();
var logger = factory.CreateLogger<Program>();

builder.AddUnifyConfiguration(logger);
```

It's also often useful to create dedicated loggers for particular events.

This can be achieved by passing a list of logger names to the `eventLoggers` parameter:

```c#
using var factory = builder.Host.AddUnifyLogging(writeToConsole: true, eventLoggers: new []{"Special", "SomethingElse", "Third"});
```

We can then use any of the `eventLoggers` using the `AsEventLogger` extension:

```c#
Log.Logger.AsEventLogger("Special").Debug("Particular event has happened");
 ```

We can also use `UnifyLogger` from dependency injection:

```c#
app.MapGet("/", (IConfiguration config, [FromServices] UnifyLogger unifyLogger) =>
{
    var thirdLogger = unifyLogger.AsEventLogger("Third");
    thirdLogger.Information("On the Homepage in third");
    return config["Unify:Secret"];
});
```

The minimum log-level for these loggers can also be configured:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Special": "Verbose"
      }
    }
  }
}
```

These logs will be written into the *App_Data\Logs* directory with the filename `[EventLoggerName]_[Date].log` making the example above "MyEventLogger_20231025.log".

These logs are subject to the same rolling interval and archival timespan as the main application log.

## Azure Insights

This library adds easy integration for logging and telemetry with Azure Insights.

Firstly, you'll need to set up a new Azure resource.

From the homepage of the Azure portal:

1. Click **Create a resource**
2. Search for **Application Insights**
3. Choose the `Subscription` and click `Create`
4. Select the `Resource Group`
5. Give it a name e.g. **SBS-AzureInsights-MyApp**
6. Click next and add `Tags` if required
7. Click `Review and Create`
8. Click `Create`

Once the deployment has been created, click `Go to resource` and copy the **Connection string**

Store this in your `secrets.json`:

```json
{
  "AzureMonitor": {
    "ConnectionString": "InstrumentationKey=e3f1ce6c-92e7...;IngestionEndpoint=https://uksouth-1.in.applicationinsights.azure.com/;LiveEndpoint=https://uksouth.livediagnostics.monitor.azure.com/;ApplicationId=c897d4e0-2d02..."
  }
}
```

In your application, pass in `useAzureInsights: true` to the `AddUnifyLogging` method:

```csharp
 var builder = WebApplication.CreateBuilder(args);
 using var factory = builder.Host.AddUnifyLogging(useAzureInsights: true);
```