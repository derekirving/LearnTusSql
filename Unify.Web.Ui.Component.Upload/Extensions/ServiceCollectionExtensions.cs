using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;
using Unify.Web.Ui.Component.Upload.TagHelpers;

#if DEBUG
using Unify.Web.Ui.Component.Upload.Stores;
#endif

// ReSharper disable once CheckNamespace
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
        var encryptedId = encryptionLib.Encrypt(unifyAppId, secret);
#if DEBUG
        services.Configure<UnifyUploadOptions>(o =>
        {
            o.BaseUrl = "/";
            o.EncryptedAppId = encryptedId;
        });

        services.AddSingleton<ITusConfigurationFactory, TusConfigurationFactoryDev>();
        services.AddSingleton<TusSqliteStore>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "unify-dev-uploads");
            var dbPath = Path.Combine(uploadsDirectory, "uploads.db");
            return new TusSqliteStore(dbPath, uploadsDirectory);
        });
        
        AddHttpClient<UnifyUploadsClientDev>(services, "http://localhost", secret, encryptedId, timeoutMinutes);
        services.AddTransient<IUnifyUploadsClient>(sp => sp.GetRequiredService<UnifyUploadsClientDev>());
        
        services.AddSingleton<IUnifyUploads, UnifyUploadsDev>();
#else
        var baseUrl = configuration["Unify:Uploads:BaseUrl"];
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

        services.Configure<UnifyUploadOptions>(o =>
        {
            o.BaseUrl = baseUrl;
            o.EncryptedAppId = encryptedId;
        });

        AddHttpClient<UnifyUploadsClient>(services, baseUrl, secret, encryptedId, timeoutMinutes);
        services.AddTransient<IUnifyUploadsClient>(sp => sp.GetRequiredService<UnifyUploadsClient>());
        services.AddSingleton<IUnifyUploads, UnifyUploads>();
#endif

        services.AddTransient<ITagHelperComponent, WebUploadsTagHelperComponent>();
        return services;
    }

    private static void AddHttpClient<T>(IServiceCollection services, string baseUrl, string secret, string encryptedId, double timeoutMinutes) where T : class
    {
        services.AddHttpClient<T>(client =>
        {
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);

            client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeaderName, secret);
            client.DefaultRequestHeaders.Add(AuthConstants.UnifyAppIdHeaderName, encryptedId);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
    }
}