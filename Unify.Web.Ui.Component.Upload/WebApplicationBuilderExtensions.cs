using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Unify.Web.Ui.Component.Upload;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddUnifyUploads(this WebApplicationBuilder builder, double timeout = 30)
    {
        builder.Services.AddHttpClient<TusApiClient>(client =>
        {
            var baseUrl = builder.Configuration["Unify:Uploads:BaseUrl"];
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeout); // Long timeout for large uploads
        });
        
        return builder;
    }
}