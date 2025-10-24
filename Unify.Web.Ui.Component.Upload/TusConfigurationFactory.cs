using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Stores;
using Microsoft.Extensions.Logging;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace Unify.Web.Ui.Component.Upload;

public sealed class TusConfigurationFactory : ITusConfigurationFactory
{
    public DefaultTusConfiguration Create(HttpContext httpContext)
    {
        var store = httpContext.RequestServices.GetRequiredService<SharedServerStore>();

        return new DefaultTusConfiguration
        {
            Store = store,
            UrlPath = "/unify/uploads",
            AllowedExtensions = new TusExtensions(TusExtensions.Creation),
            MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
            UsePipelinesIfAvailable = true,
            Events = new Events
            {
                OnBeforeWriteAsync = async ctx =>
                {
                    var file = await ctx.GetFileAsync();
                    var metadata = await file.GetMetadataAsync(ctx.CancellationToken);
                    
                    metadata.TryGetValue("name", out var fileName);
                    metadata.TryGetValue("zoneId", out var zoneId);
                    metadata.TryGetValue("uploadId", out var uploadId);
                    metadata.TryGetValue("appId", out var appId);

                    if (fileName == null || zoneId == null || uploadId == null || appId == null)
                    {
                        ctx.FailRequest(HttpStatusCode.BadRequest, "Validation Failed: MetaData missing");
                        return;
                    }
                },
                OnFileCompleteAsync = async ctx =>
                {
                    var file = await ctx.GetFileAsync();
                    var metadata = await file.GetMetadataAsync(ctx.CancellationToken);

                    // if (metadata.TryGetValue("sessionId", out var sessionIdMeta))
                    // {
                    //     var sessionId = sessionIdMeta.GetString(Encoding.UTF8);
                    //     await store.AssociateFileWithSessionAsync(file.Id, sessionId, ctx.CancellationToken);
                    // }
                    
                    // if (metadata.TryGetValue("appId", out var appIdMetadata))
                    // {
                    //     var appId = appIdMetadata.GetString(Encoding.UTF8);
                    //     await store.SetAppIdAsync(file.Id, appId, ctx.CancellationToken);
                    // }

                    var endPoint = $"{ctx.HttpContext.Request.PathBase}/unify/uploads/{ctx.FileId}";
                    ctx.HttpContext.Response.Headers.Append("Content-Location", endPoint);

                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<TusConfigurationFactory>>();
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