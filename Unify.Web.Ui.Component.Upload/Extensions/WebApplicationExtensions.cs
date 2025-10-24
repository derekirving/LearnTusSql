#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using tusdotnet;
#endif

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Unify.Web.Ui.Component.Upload.Interfaces;

// ReSharper disable once CheckNamespace
namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationExtensions
{
    public static WebApplication MapUnifyUploads(this WebApplication app)
    {
        #if DEBUG
        app.UseTus(ctx => ctx.RequestServices.GetRequiredService<ITusConfigurationFactory>().Create(ctx));
        app.MapGet("/unify/upload", () => "Upload endpoint");
        #endif
        
        app.MapGet("/unify/download/{fileId}", async (HttpContext ctx, IUnifyUploads unifyUploads, string fileId) =>
        {
            var (stream, contentType, fileName) = await unifyUploads.DownloadFileAsync(fileId, ctx.RequestAborted);
            return Results.File(stream, contentType, fileName);
        }).WithName("UnifyDownload");
        
        return app;
    }
}