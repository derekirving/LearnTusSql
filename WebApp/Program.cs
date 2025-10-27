using Serilog;
using Unify;
using Unify.Logging;
using Unify.Web.Ui.Component.Upload;
using WebApp.Data;

ILogger<Program>? logger = null;

try
{
    var builder = WebApplication.CreateBuilder(args);
    using var factory = builder.Host.AddUnifyLogging();
    logger = factory.CreateLogger<Program>();

    builder.Host.AddUnifyConfiguration("TestApp-123");
    builder.AddUnifyUploads("TestApp-123");

    builder.Services
        .AddDbContext<AppDbContext>()
        .AddRazorPages();

    var app = builder.Build();

    app.UseStaticFiles();
    app.MapRazorPages();
    app.MapUnifyUploads();
    app.Run();
}
catch (Exception ex)
{
    logger?.LogCritical(ex, "Host for UnifyWebApp terminated unexpectedly");
    Console.WriteLine(ex.Message);
}
finally
{
    logger?.LogInformation("Shutdown of UnifyWebApp complete");
    Log.CloseAndFlush();
}