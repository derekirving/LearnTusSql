#if DEBUG
using tusdotnet;
#endif

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unify.Logging;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Interfaces;
using ILogger = Serilog.ILogger;

// ReSharper disable once CheckNamespace
namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationExtensions
{
    public static WebApplication MapUnifyUploads(this WebApplication app, bool minifyInDev = false)
    {
        ILogger? logger = null;
        
        var loggerFactory = app.Services.GetService<UnifyLogger>();
        if (loggerFactory != null)
        {
            logger = loggerFactory.AsEventLogger(UploadConstants.UploadsEventLogger);
            logger.Information("Logger enabled {service}", nameof(UnifyUploads));
        }
        
        var memoryCache = app.Services.GetRequiredService<IMemoryCache>();
        
#if DEBUG
        app.UseTus(ctx => ctx.RequestServices.GetRequiredService<ITusConfigurationFactory>().Create(ctx));
        app.MapGet("/unify/upload", () => "Upload endpoint");
#endif

        app.MapUnifyUploads(memoryCache, app.Environment, logger, minifyInDev);
        
        return app;
    }

    private static void MapUnifyUploads(this IEndpointRouteBuilder endpoints, IMemoryCache memoryCache,
        IWebHostEnvironment appEnvironment, ILogger? logger, bool minifyInDev)
    {
        endpoints.MapGet("/unify/uploads/static/style.css", async context =>
        {
            var content = await GetEmbeddedAsset("Files.upload.css", memoryCache, appEnvironment, logger, minifyInDev);
            context.Response.ContentType = "text/css";
            logger?.Debug("Serving {file} from url {url}", "Files.upload.css", context.Request.Path);
            await context.Response.WriteAsync(content);
        });
        
        endpoints.MapGet("/unify/uploads/static/script.js", async context =>
        {
            var content = await GetEmbeddedAsset("Files.upload.js", memoryCache, appEnvironment, logger, minifyInDev);
            context.Response.ContentType = "text/javascript";
            logger?.Debug("Serving {file} from url {url}", "Files.upload.js", context.Request.Path);
            await context.Response.WriteAsync(content);
        });
        
        endpoints.MapGet("/unify/download/{fileId}", async (HttpContext ctx, IUnifyUploads unifyUploads, string fileId) =>
        {
            var (stream, contentType, fileName) = await unifyUploads.DownloadFileAsync(fileId, ctx.RequestAborted);
            return Results.File(stream, contentType, fileName);
        }).WithName("UnifyDownload");
    }

    // TODO: This should be included in Unify.Web as a similar thing is used by Unify.Messaging.WebPush
    private static async Task<string> GetEmbeddedAsset(string fileName, IMemoryCache memoryCache,
        IWebHostEnvironment appEnvironment, ILogger? logger, bool minifyInDev)
    {
        logger?.Debug("Getting or caching {filename}", fileName);
        
        var asset = await memoryCache.GetOrCreateAsync(fileName, async entry =>
        {
            entry.Priority = CacheItemPriority.Low;
            
            var extension = Path.GetExtension(fileName);
            if (minifyInDev || appEnvironment.IsProduction())
            {
                fileName = fileName.Replace(extension, ".min" + extension);
            }
            
            var assembly = typeof(UnifyUploads).Assembly;
            var resourceName = $"{UploadConstants.NameSpace}.{fileName}";
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            
            if (resourceStream == null)
            {
                logger?.Debug("Resource stream not found {name}", resourceName);
                return string.Empty;
            }
            
            entry.Priority = CacheItemPriority.NeverRemove;
            
            using var reader = new StreamReader(resourceStream);
            var content = await reader.ReadToEndAsync();
            
            return content;
            
        });
        
        return string.IsNullOrEmpty(asset) ? $"<!-- Unify-Web-Uploads Error: {fileName} returned empty -->" : asset;
    }
}