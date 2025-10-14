namespace Unify.Uploads.Api;

using System.Text;
using Microsoft.Extensions.Logging;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

public interface ITusConfigurationFactory
{
    DefaultTusConfiguration Create(HttpContext httpContext);
}

public sealed class TusConfigurationFactory(ILogger<TusConfigurationFactory> logger) : ITusConfigurationFactory
{
    private readonly ILogger<TusConfigurationFactory> _logger = logger;

    public DefaultTusConfiguration Create(HttpContext httpContext)
    {
        var store = httpContext.RequestServices.GetRequiredService<TusSqlServerStore>();

        return new DefaultTusConfiguration
        {
            Store = store,
            UrlPath = "/files",
            Events = new Events
            {
                OnFileCompleteAsync = async ctx =>
                {
                    var file = await ctx.GetFileAsync();
                    var metadata = await file.GetMetadataAsync(ctx.CancellationToken);

                    if (metadata.TryGetValue("sessionId", out var sessionIdMeta))
                    {
                        var sessionId = sessionIdMeta.GetString(Encoding.UTF8);
                        await store.AssociateFileWithSessionAsync(file.Id, sessionId, ctx.CancellationToken);
                    }

                    // if (metadata.TryGetValue("appId", out var appIdMetadata))
                    // {
                    //     var appId = appIdMetadata.GetString(Encoding.UTF8);
                    //     await store.SetAppIdAsync(file.Id, appId, ctx.CancellationToken);
                    // }

                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    Log.FileUploadCompleted(logger, file.Id);
                },
                OnBeforeCreateAsync = async ctx =>
                {
                    if (ctx.UploadLength > 5_000_000_000) // 5GB
                        ctx.FailRequest("File size exceeds maximum allowed size of 5GB");

                    // if (!ctx.HttpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
                    // {
                    //     ctx.FailRequest("Missing API key");
                    // }

                    await Task.CompletedTask;
                }
            }
        };
    }
}