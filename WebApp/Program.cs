using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using Unify.Validation;
using Unify.Web.Ui.Component.Upload;
using Unify.Web.Ui.Component.Upload.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpClient<TusApiClient>(client =>
{
    var tusApiBaseUrl = builder.Configuration["TusApi:BaseUrl"] ?? "https://localhost:7000";
    client.BaseAddress = new Uri(tusApiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(30); // Long timeout for large uploads
});

// builder.Services.AddSingleton<TusDiskStorageOptionHelper>();

// builder.Services.AddSingleton<TusSqliteStore>(_ =>
//     new TusSqliteStore(
//         databasePath: Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "tusfiles-sqlite.db"),
//         uploadDirectory: Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "tusfiles-sqlite")
//     )
// );

// builder.Services.AddSingleton<IUnifyUploads, UnifyUploads>();
//
// builder.Services.AddUnifyBinaryValidation(["txt", "pdf", "docx"]);
//
// var sqliteStore = new TusSqliteStore(
//     databasePath: Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "tusfiles-sqlite.db"),
//     uploadDirectory: Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "tusfiles-sqlite")
// );
//
// var tusConfig = new DefaultTusConfiguration
// {
//     Store = sqliteStore,
//     UrlPath = "/unify/uploads",
//     Events = new Events
//     {
//         OnFileCompleteAsync = async eventContext =>
//         {
//             var file = await eventContext.GetFileAsync();
//             var metadata = await file.GetMetadataAsync(eventContext.CancellationToken);
//
//             // Get the file path
//             var store = eventContext.Store as TusSqliteStore;
//             // You can now process the completed file
//
//             Console.WriteLine($"File {file.Id} completed!");
//         }
//     }
// };
//
// builder.Services.AddSingleton(tusConfig);

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

//app.UseTus(httpContext => httpContext.RequestServices.GetRequiredService<DefaultTusConfiguration>());

// app.UseTus(httpContext => new DefaultTusConfiguration
// {
//     Store = httpContext.RequestServices.GetRequiredService<TusSqlServerStore>(),
//     UrlPath = "/files",
//     Events = new Events
//     {
//
//     }
// });

app.Run();