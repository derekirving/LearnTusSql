using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;
using Unify.Web.Ui.Component.Upload.TagHelpers;

#if DEBUG
using Microsoft.AspNetCore.Hosting;
using Unify.Web.Ui.Component.Upload.Stores;
#endif

#if RELEASE
using Unify.Web.Ui.Component.Upload.Constants;
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

        services.AddSingleton<IUnifyUploads, UnifyUploads>();
        services.AddTransient<ITagHelperComponent, WebUploadsTagHelperComponent>();

        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once RedundantAssignment
        var baseUrl = "/";
#if RELEASE
        baseUrl = configuration["Unify:Uploads:BaseUrl"];
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);
#endif
        services.Configure<UnifyUploadOptions>(o =>
        {
            o.BaseUrl = baseUrl;
            o.EncryptedAppId = encryptedId;
        });
#if DEBUG
        services.AddSingleton<DbConnectionFactory>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            return new DbConnectionFactory(env);
        });

        services.AddSingleton<ITusConfigurationFactory, TusConfigurationFactory>();
        
        services.AddSingleton<SharedServerStore>(sp =>
        {
            var connectionFactory = sp.GetRequiredService<DbConnectionFactory>();
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "unify-dev-uploads");
            
            return new SharedServerStore(configuration, encryptionLib, uploadsDirectory, connectionFactory);
        });
        
        services.AddTransient<IUnifyUploadsClient, UnifyUploadsClientDev>();
        
        services.AddHostedService<TusCleanupService>();
#else
        services.AddHttpClient<UnifyUploadsClient>(client =>
        {
            client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeaderName, secret);
            client.DefaultRequestHeaders.Add(AuthConstants.UnifyAppIdHeaderName, encryptedId);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
        
        services.AddTransient<IUnifyUploadsClient>(sp => sp.GetRequiredService<UnifyUploadsClient>());
#endif
        return services;
    }
}