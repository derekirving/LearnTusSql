using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;
using Unify.Web.Ui.Component.Upload.TagHelpers;

#if DEBUG
using Unify.Encryption;
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

        services.AddSingleton<DbConnectionFactory>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var conf = provider.GetRequiredService<IConfiguration>();
            return new DbConnectionFactory(env, conf);
        });
        
        services.Configure<UnifyUploadOptions>(o =>
        {
            o.BaseUrl = "/";
            o.EncryptedAppId = encryptedId;
        });

        services.AddSingleton<ITusConfigurationFactory, TusConfigurationFactoryDev>();
        
        services.AddSingleton<SharedServerStore>(sp =>
        {
            //var encryption = sp.GetRequiredService<IUnifyEncryption>();
            var encryption = encryptionLib;
            var connectionFactory = sp.GetRequiredService<DbConnectionFactory>();
            
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "unify-dev-uploads");
            
            return new SharedServerStore(configuration, encryption, uploadsDirectory, connectionFactory);
        });
        
        //AddHttpClient<UnifyUploadsClientDev>(services, "http://localhost", secret, encryptedId, timeoutMinutes);
        //services.AddTransient<IUnifyUploadsClient>(sp => sp.GetRequiredService<UnifyUploadsClientDev>());
        services.AddTransient<IUnifyUploadsClient, UnifyUploadsClientDev>();
        services.AddSingleton<IUnifyUploads, UnifyUploadsDev>();
#else
        var baseUrl = configuration["Unify:Uploads:BaseUrl"];
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

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
        
        services.AddTransient<IUnifyUploadsClient>(sp => sp.GetRequiredService<UnifyUploadsClient>());
        services.AddSingleton<IUnifyUploads, UnifyUploads>();
#endif

        services.AddTransient<ITagHelperComponent, WebUploadsTagHelperComponent>();
        return services;
    }
}