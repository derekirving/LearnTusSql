using Microsoft.AspNetCore.Builder;
#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using tusdotnet;
using Unify.Web.Ui.Component.Upload.Interfaces;
#endif

// ReSharper disable once CheckNamespace
namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationExtensions
{
    public static WebApplication MapUnifyUploads(this WebApplication app)
    {
        #if DEBUG
        app.UseTus(ctx => ctx.RequestServices.GetRequiredService<ITusConfigurationFactory>().Create(ctx));
        app.MapGet("/upload", () => "Upload endpoint");
        #endif
        
        return app;
    }
}