using tusdotnet;
using Unify;
using Unify.Encryption;
using Unify.Uploads.Api;
using Unify.Uploads.Api.Authentication;
using Unify.Web.Ui.Component.Upload;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Stores;

var builder = WebApplication.CreateBuilder(args);
builder.AddUnifyConfiguration();

builder.Services.AddUnifyEncryption();

var allowedOrigins = builder.Configuration.GetSection("TusSettings:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null)
{
    throw new Exception("No allowed origins are specified");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Upload-Offset", "Upload-Length", "Tus-Resumable", "Location", "Content-Location");
    });
});

builder.Services.AddSingleton<DbConnectionFactory>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var conf = provider.GetRequiredService<IConfiguration>();
    return new DbConnectionFactory(env, conf);
});

builder.Services.AddSingleton<ITusConfigurationFactory, TusConfigurationFactory>();

builder.Services.AddSingleton<SharedServerStore>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var encryption = sp.GetRequiredService<IUnifyEncryption>();
    var connectionFactory = sp.GetRequiredService<DbConnectionFactory>();

    var uploadsDirectory = configuration["TusSettings:UploadDirectory"];
    ArgumentException.ThrowIfNullOrEmpty(uploadsDirectory);

    return new SharedServerStore(configuration, encryption, uploadsDirectory, connectionFactory);
});

builder.Services.AddHostedService<TusCleanupService>();

var app = builder.Build();

app.UseCors("ApiPolicy");
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseTus(ctx => ctx.RequestServices.GetRequiredService<ITusConfigurationFactory>().Create(ctx));
app.MapEndpoints();
app.Run();