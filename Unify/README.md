# Unify

**NOTE** The way the base url for an application is determined may be automated in the future.

The safest thing to do currently is to manually set the base url in appsettings. This is what the Unify templates do:

```json
// appsettings.json
{
  "Unify__AppBaseUrl" : "https://www.sbs.strath.ac.uk/yourapp"
}
// appsettings.development.json
{
  "Unify__AppBaseUrl" : "https:/localhost:8001"
}
```

## Unify Application Template ID and Secrets

This package is installed as part of every [Unify Template](../../Templates/DotNet).

In a Unify template with an "Application" layer, the `UserSecretsId` property is included in the application’s `csproj` file. This ID is generated when a new project is created and can be modified in the `csproj` file at any time if necessary. During compilation, this property generates a `unify.app.id` file in the root of the solution, which corresponds to the .NET **UserSecrets** folder.

This configuration enables all "presentation" style projects (e.g., Web, API, CLI) that reference the Application template to utilise this secret configuration without any additional setup. It also makes it possible to use these **UserSecrets** in both development and production without compromising security. 

## Non Unify Application Template

You can continue to use the Unify package to manage secret configuration outside of a Unify Application as you always could by manually adding a `<UserSecretsId/> property to your csproj file.

However, to use these **UserSecrets** for **production**, you will need to manually pass the `UserSecretsId` to the method:

```c#
builder.Host.AddUnifyConfiguration("[Your_Projects_User_Secrets_ID]");
```

## Setup

In the unlikely event that your app doesn't need any configuration of its own, simply call:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.AddUnifyConfiguration();
```

### Strongly typed configuration

If your app does require some configuration, do this instead:

Create a class to hold configuration inheriting from `UnifyBaseConfiguration`

```c#
using FluentValidation;
using Unify;

public class ApplicationOptions : UnifyBaseConfiguration
{
    public string BaseAddress { get; set; } = string.Empty;
}
```

Create a corresponding validator:

```c#
public class ApplicationOptionValidator : AbstractValidator<ApplicationOptions>
{
    public ApplicationOptionValidator()
    {
        RuleFor(x => x.BaseAddress).NotNull().NotEmpty();
    }
}
```

Add the configuration to `appsettings.json` and/or `secrets.json`

```json
{
  "ApplicationOptions": {
  	"BaseAddress": "https://domain.com"
  }
}
```

No misconfiguration surprise at runtime. An `AbortStartupException` is s thrown as soon as the application is started if any configuration is invalid.


Here is how to wire this up in various different scenarios:

## Unify Application Layer

In the initialisation class:

```c#
builder.AddUnifyConfiguration<Domain.Application>(Unify.Generated.Configuration.UserSecretsId, logger);
```


## Web or Blazor Server App

With the inclusion of the [Unify.Web](../Unify.Web) package.

```c#
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Validated partial configuration is available here
var applicationOptions = builder.AddUnifyConfiguration<ApplicationOptions>();

var app = builder.Build();

app.UseUnifyWeb();

// Validated full configuration is available here from dependency injection 
app.MapGet("/", (IOptionsMonitor<ApplicationOptions> options) => $"Hello World!  {options.CurrentValue.BaseAddress} {options.CurrentValue.DefaultCulture} {options.CurrentValue.GitHash}");

app.Run();
```

\* Partial means anything that doesn't need `HttpContext` i.e a request to populate the configuration object 

## Cli App using Cocona or Console

**NOTE** the `AddUnifyConfiguration` method requires the process to be running as the [NetworkService account](https://learn.microsoft.com/en-us/windows/win32/services/networkservice-account) to access the configuration files from the IDrive.

This works fine on a developer machine as that machine normally has access to the IDrive through a drive mappping.

In an IIS WebApp, Windows Service or Scheduled Task, the process can be configured to run as the NetworkService account (in IIS through the [Application Pool identity](https://learn.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities)).

However to run a Cli App form a command prompt on a machine that does not have direct access to the IDrive you can use [PsExec.exe](https://docs.microsoft.com/en-us/sysinternals/downloads/psexec) from SysInternals, by running from an elevated command prompt:

```bash
psexec -i -u "nt authority\network service" cmd.exe 
```

With that out the way, here is how a basic Cli App with validated configuration might look:

```c#
using Microsoft.Extensions.Options;
var builder = CoconaApp.CreateBuilder();

//Configuration is available here but it is not validated
var applicationOptions = builder.Host.AddUnifyConfiguration<ApplicationOptions>();

var app = builder.Build();

// In a CoconaApp, Validation has to happen after the app is built.
var validator = app.Services.GetRequiredService<IValidator<ApplicationOptions>>();
var validationResult = validator.Validate(applicationOptions);
if (!validationResult.IsValid)
{
    var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
    var exceptionMessage = errors.Aggregate((error, foo) => $"{error} \n");
    throw new AbortStartupException(exceptionMessage);
}

app.AddCommand((IValidator<ApplicationOptions> optionsValidator, IOptionsMonitor<ApplicationOptions> options) =>
{
    if(!optionsValidator.Validate(applicationOptions).IsValid)
    {
        throw new AbortStartupException("Invalid application options");
    }
    
    Console.WriteLine( $"Hello World!  {options.CurrentValue.BaseAddress} {options.CurrentValue.DefaultCulture} {options.CurrentValue.GitHash}");
    
});

await app.RunAsync();
```

**NOTE** For Cocona (CLI) Apps

Due to an issue with `IStartupFilter` compatibility, configuration is bound/configured but not validated.

The work around is this:

```c#
var builder = CoconaApp.CreateBuilder();
var applicationOptions = builder.Services.AddApplicationOptions<ApplicationOptions>(builder.Configuration);

// ApplicationOptions is available here but is not validated

var app = builder.Build();

var validator = app.Services.GetRequiredService<IValidator<ApplicationOptions>>();
var validationResult = validator.Validate(applicationOptions);
if (!validationResult.IsValid)
{
    var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
    var exceptionMessage = errors.Aggregate((error, foo) => $"{error} \n");
    throw new AbortStartupException(exceptionMessage);
}

// ApplicationOptions is available and now validated

It can also be used with or without the above using dependency injection in Cocona commands:

app.AddCommand(async (ILogger<Program> logger, IValidator<ApplicationOptions> validator, IOptionsMonitor<ApplicationOptions> options) =>
{
	if(!validator.Validate(applicationOptions).IsValid)
    {
        throw new AbortStartupException("Invalid application options");
    }
});
```

### Configuration Order

The configuration is loaded in the following order:


| File                                   | Reload on Change            |
|----------------------------------------|-----------------------------|
| appsettings.json                       | true                        |
| appsettings.{env.EnvironmentName}.json | true                        |
| secrets.json                           | false                       | 
| global.json                            | false                       |
| secrets.json*                          | false                       |


***During development only** your secrets file is re-loaded **after** global.json, permitting you to overwrite any global secrets/connectionsstrings for local alternatives.



## Connection Strings

Unify provides an easy way to obtain and work with database connection strings without requiring any usernames/passwords.

### Development Databases

During development, you might be using a local database so configure the connection strings in `appsettings.Development.json`:

```json
{
    "ConnectionStrings" : {
        "MyDbOne" : "Server=(localdb)\\mssqllocaldb;Database=MyDevDbOne;Trusted_Connection=True;"
    }
}
```
This can safely be committed to source control.

If the database is to be re-used between applications, it should be stored in the global `connectionstrings.json` file.

### Production Databases

**NOTE** Your development machine may not be able to access existing live databases due to firewall restrictions on the database server but this can be configured if required.

To connect to production databases, you will request them by name in `appsettings.json` or in your `secrets.json` file if you prefer.

```json
{
  "Unify" : {
    "RequiredConnectionStrings": ["LiveDbOne", "LiveDbTwo"]
  }
}
```

This can also safely be committed to source control.

You can then work with them as you would any other connection string. For example:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.AddUnifySqlContext<LiveDbOneContext>("LiveDbOne");
```

or

```c#
public class MyClass
{   
    private readonly IConfiguration _configuration;

    public MyClass(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void MyMethod()
    {
        var connectionString = _configuration.GetConnectionString("LiveDbOne");
    }
}
```

## Happy Path

This package provides the [Happy Path Extension](Extensions/ResultExtensions.cs) for Console/Cli only. For Web/WebApi projects Use [Unify.Web](../Unify.Web) / [Unify.Web.Api](../Unify.Web.Api) instead.

## GitHash

The Unify package provides targets that write the git hash and remote repository details to the assembly attributes that are later used by Unify endpoints.

To disable this behaviour, set `GitHasEnabled` to `false` in the .csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <GitHashEnabled>False</GitHashEnabled>
    </PropertyGroup>
</Project>
```

This can be used in conjunction with endpoints e.g.

```c#
app.MapGet("/version", () => Results.Json(BuildInfo.Report());
```

or in a Console app:

```c#
var buildInfo = BuildInfo.Report();
Console.WriteLine($"Version: {buildInfo.AssemblyVersion}");
Console.WriteLine($"GitRepo: {buildInfo.GitRepo}");
Console.WriteLine($"GitHash: {buildInfo.GitHash}");
```

For a WebApp, you can use the `BuildInfo.ReportAsHtml()` method which will return an html string:

```c#
app.MapGet("/version", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(BuildInfo.ReportAsHtml());
});
```
The HTML returned will look like this:

```html
Runtime .NET 6.0.16. Version: 0.1.0 Commit: <a href='https://github.com/org/repo/commit/3ed11b4'>3ed11b4</a> (dirty)
```

## Mapping

This package provides source generated mapping via the [Mapperly](https://mapperly.riok.app/) package.

Here is a very simple example of how to start using it:

Given a model...

```csharp
public class Student
{
    public int Id { get; set; }
    public string? ForeName { get; set; }
    public string? LastName { get; set; }
}
```

...and a Data Transfer Object:

```csharp
public class StudentDto
{
    public required string ForeName { get; set; }
    public required string SurName { get; set; }
}
```

We can create the mapper...

```csharp
[Mapper]
public partial class StudentMapper
{
    [MapperIgnoreSource(nameof(Student.Id))]
    [MapProperty(nameof(Student.LastName), nameof(StudentDto.SurName))]
    public partial StudentDto MapToStudentDto(Student student);
}
```

... and make use of it:

```csharp
var student = new Student
{
    Id = 1,
    ForeName = "Bob",
    LastName = "Smith"
};

var mapper = new StudentMapper();
var dto = mapper.MapToStudentDto(student);

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(dto));
```

The default is not to emit the generated mappings to your project, however you can override this in your `.csproj` file:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
``` 

## Extensions

This package provides extensions used by other Unify packages but can also be used directly.

### String Extensions

```c#
.ToSafeFilename(string replacement = "_");
.RemoveLineBreaks();
.StripHtml();
.StripHTMLFromstr();
.ReplaceNewLineWithBr();
.ReplaceSpaceCharacters(string replacement = "_");
.RemoveDoubleSpaceCharacters();
.CutLongString(int length);
.RemoveQuotationMarks();
.ToPermaLink(int length = 100);
.ToNonBreakingSubstring(int startIndex, int length);

int characterCount = "Mary Had A Little Lamb".CountChars('L);
```

*NB* Many of these string extensions could be optimised with the use of [Span<T>](https://docs.microsoft.com/en-us/dotnet/api/system.span-1?view=net-6.0)


### DateTime Extensions

```csharp
.GetFirstDayOfWeek()
```

### EnumerableExtensions

#### Batch

Loop through IEnumerable in batches

```c#
const int batchSize = 10;
IEnumerable<int> tasks = GetListOfManyThings();

foreach (var task in tasks.Batch(batchSize))
{
    // do something 10 items at a time
}
```

#### ToOxfordComma

When displaying values in code, you may want to format it for your users naturally, and picking the Oxford comma is the most natural form.

```c#
var items = new[] { "Cats" };
var result = items.ToOxfordComma(); // "Cats"

var TwoItems = new[] { "Cats", "Dogs" };  
result = TwoItems.ToOxfordComma();  // "Cats and Dogs"

var ThreeItems = new[] { "Cats", "Dogs", "Capybara" };  
result = ThreeItems.ToOxfordComma(); // "Cats, Dogs, and Capybara"
```

### Directory Extensions

#### DeepCopy

Copy the Entire Contents of a Directory

```c#
var sourceDir = new DirectoryInfo(sourcePath);
sourceDir.DeepCopy(destinationPath, true);
```
## Helpers

### ValueStringBuilder

Build strings faster and with less allocations than the standard StringBuilder.

```c#
var fast = new ValueStringBuilder();
        fast.AppendLine("Hello World");
        fast.AppendLine("Here some other text");
        fast.AppendLine("And again some other text as well for good measure");
        return fast.ToString();
```

### SemaphoreSlim

No need to use the try/catch block with this extension.

```csharp

public class SomeClass
{
    private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
    
    private async Task SomeAsyncMethod()
    {
        using (SemaphoreSlim.UseWaitAsync())
        {
            // Your code here
        }
    }

    private void SomeMethod()
    {
        using (SemaphoreSlim.UseWait())
        {
            // Your code here
        }
    }
}
```

**NOTE** You may instead prefer to use `BulkHead` from the [Unify.Resilience](../Unify.Resilience#bulkhead) package
which offers more flexibility than the above technique.

### Async Safe Fire and Forget

An extension method to safely fire-and-forget a Task.

SafeFireAndForget allows a Task to safely run on a different thread while the calling thread does not wait for its completion.

```c#
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        
        ExampleAsyncMethod()
            .SafeFireAndForget(onException: ex => logger.LogError("{message}", ex.Message));
    }

    public void OnGet()
    {

    }

    public async Task ExampleAsyncMethod()
    {
        await Task.Delay(1000);
        throw new Exception("ExampleAsyncMethod failed");
    }
}
```

### FlatDictionary

Convert a poco to a Dictionary

```csharp

var myClass = new MyClass
        {
            Boolean = true,
            Guid = Guid.NewGuid(),
            Integer = 100,
            String = "string",
            MyNestedClass = new MyNestedClass
            {
                Boolean = true,
                String = "string",
                Guid = Guid.NewGuid(),
                Integer = 100
            }
        };

var result = FlatDictionary.Create(myClass);


// or add a prefix
var result = FlatDictionary.Create(myClass, "MyObject_");
 ```

### FastGuid

10 times faster than Guid.NewGuid()

```c#
Guid guid = FastGuid.NewGuid();
// and
Guid guid = FastGuid.NewSqlServerGuid();
```

**Legacy Documentation**

This still works but it's much better to use the strongly typed, validate configuration as described above.

In your program file, add Unify Configuration:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Host
    .ConfigureAppConfiguration((context, config) => config.AddUnifyConfiguration(context));
    
var app = builder.Build();
app.UseUnifyWeb();
```

## .NET Framework

**NB** .NET Framework 4.7.2 is the minimum supported version for Unify.

`Install-Package Unify`

If you haven't done so already, you'll need to add an OWIN startup file.

```c#
using Microsoft.Extensions.Configuration;
using Microsoft.Owin;
using Owin;
using Unify.Configuration.NET4;
using Unify.NET4;

[assembly: OwinStartup(typeof(MyApp.Startup))]
namespace MyApp
{
    public class Startup
    {
        public static IConfigurationRoot Config { get; set; }
        
        public void Configuration(IAppBuilder app)
        {
            app.UseUnify();
            Config = app.UseUnifyConfiguration();
        }
    }
}
```

Now from anywhere in your application you can read configuration values:

```c#
var globalConfigItem = Startup.Config["Unify:Secret"]; // From Unify Global Configuration
var localConfigItem = Startup.Config["webpages:Version"]; // From web.config
```

The `app.UseUnify()` method registers some [middleware](./NET4/UnifyMiddleware.cs) and an [actionfilter](./NET4/RedirectingAction.cs) that facilitate correct functionality when running a .NET 4 application across the proxy.

A [helper method](./NET4/Helpers.cs) is also provided in cases where a redirect needs to be "proxy aware" for example in **Global.asax** when redirecting to an error page:

```c#
public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Error(object sender, EventArgs e)
    {
        var proxyPathBase = Unify.NET4.Helpers.GetProxyUrl(HttpContext.Current);
        System.Web.HttpApplication.Server.Transfer(proxyPathBase + "/Error.aspx");
    }
}
```
**NOTE** You can safely use the `GetProxyUrl()` method even if not running through the proxy server.
