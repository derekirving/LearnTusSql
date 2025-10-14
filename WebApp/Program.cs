using Unify;
using Unify.Web.Ui.Component.Upload;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddUnifyConfiguration("TestApp-123");
builder.Services
    .AddUnifyUploads(builder.Configuration, "TestApp-123")
    .AddRazorPages();
//builder.AddUnifyUploads("TestApp-123");

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();
app.Run();