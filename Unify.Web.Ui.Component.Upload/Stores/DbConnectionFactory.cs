using System.Data;
using Microsoft.AspNetCore.Hosting;
#if RELEASE
using Microsoft.Data.SqlClient;
#endif
#if DEBUG
using Microsoft.Data.Sqlite;
#endif
using Microsoft.Extensions.Configuration;

namespace Unify.Web.Ui.Component.Upload.Stores;

public class DbConnectionFactory(IWebHostEnvironment environment, IConfiguration configuration)
{
    public IDbConnection CreateConnection()
    {
#if DEBUG
        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "unify-dev-uploads");
        var dbPath = Path.Combine(uploadsDirectory, "uploads.db");
        return new SqliteConnection("Data Source=" + dbPath);
#elif RELEASE
    var connectionString = configuration.GetConnectionString("TusDatabase");
    ArgumentException.ThrowIfNullOrEmpty(connectionString);
    return new SqlConnection(connectionString);
#endif
    }
}