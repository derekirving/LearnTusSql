using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unify.Web.Ui.Component.Upload.TagHelpers;

namespace Unify.Web.Ui.Component.Upload;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnifyUploads(this IServiceCollection services, IConfiguration configuration,
        string unifyAppId, double timeoutMinutes = 5)
    {
        var secret = configuration["Unify:Secret"];
        ArgumentException.ThrowIfNullOrEmpty(secret);
        
        var baseUrl = configuration["Unify:Uploads:BaseUrl"];
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);
        
        UnifyEncryptionProvider.Initialise(configuration);
        var encryptionLib = UnifyEncryptionProvider.Instance;
        
        var encryptedId = encryptionLib.Encrypt(unifyAppId, secret);
        
        services.Configure<UnifyUploadOptions>(o =>
        {
            o.BaseUrl = baseUrl;
            o.EncryptedAppId = encryptedId;
        });
        
        services.AddHttpClient<UnifyUploadsClient>(client =>
        {
            
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            
            client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeaderName, secret);
            client.DefaultRequestHeaders.Add(AuthConstants.UnifyAppIdHeaderName, encryptedId);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
        
        services.AddTransient<ITagHelperComponent, WebUploadsTagHelperComponent>();
        
        services.AddSingleton<IUnifyUploads, UnifyUploads>();
        
        return services;
    }
}