using Unify;
using Unify.Web.Ui.Component.Upload;
using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddUnifyConfiguration("TestApp-123");
builder.AddUnifyUploads("TestApp-123");

builder.Services
    .AddDbContext<AppDbContext>()
    .AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();
app.MapUnifyUploads();
app.Run();