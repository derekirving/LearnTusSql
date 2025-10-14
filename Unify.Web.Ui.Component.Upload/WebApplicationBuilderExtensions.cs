using Microsoft.AspNetCore.Builder;

namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddUnifyUploads(this WebApplicationBuilder builder, string unifyAppId, double timeoutMinutes = 5)
    {
        builder.Services.AddUnifyUploads(builder.Configuration, unifyAppId, timeoutMinutes);
        return builder;
    }
}