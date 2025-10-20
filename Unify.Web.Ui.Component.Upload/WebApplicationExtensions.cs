using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using tusdotnet;
using Unify.Web.Ui.Component.Upload.Interfaces;

namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationExtensions
{
    public static void MapUnifyUploads(this WebApplication app)
    {
        #if DEBUG
        app.UseTus(ctx => ctx.RequestServices.GetRequiredService<ITusConfigurationFactory>().Create(ctx));
        app.MapGet("/upload", () => "Upload endpoint");
        #endif
    }
}