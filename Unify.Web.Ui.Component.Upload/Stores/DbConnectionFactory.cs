using System.Data;

#if DEBUG
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
#endif

#if RELEASE
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
#endif

namespace Unify.Web.Ui.Component.Upload.Stores;

#if DEBUG
public class DbConnectionFactory(IWebHostEnvironment environment)
{
    public IDbConnection CreateConnection()
    {
        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "unify-dev-uploads");
        var dbPath = Path.Combine(uploadsDirectory, "uploads.db");
        return new SqliteConnection("Data Source=" + dbPath);
    }
}
#else
public class DbConnectionFactory(IConfiguration configuration)
{
    public IDbConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("TusDatabase");
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return new SqlConnection(connectionString);
    }
}
#endif