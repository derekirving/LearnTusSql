using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Unify.Web.Ui.Component.Upload;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnifyUploads(this IServiceCollection services, IConfiguration configuration,
        string unifyAppId, double timeoutMinutes = 5)
    {
        var secret = configuration["Unify:Secret"];
        ArgumentException.ThrowIfNullOrEmpty(secret);
        
        UnifyEncryptionProvider.Initialise(configuration);
        var encryptionLib = UnifyEncryptionProvider.Instance;
        
        var encryptedId = encryptionLib.HashPassword(unifyAppId, secret);
        
        services.AddHttpClient<TusApiClient>(client =>
        {
            var baseUrl = configuration["Unify:Uploads:BaseUrl"];
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            
            client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeaderName, secret);
            client.DefaultRequestHeaders.Add(AuthConstants.UnifyAppIdHeaderName, encryptedId);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
        
        return services;
    }
}