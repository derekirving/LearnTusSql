using Microsoft.EntityFrameworkCore;

namespace WebApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Post> Posts { get; set; }
    private string DbPath { get; }

    public AppDbContext()
    {
        var path = Environment.CurrentDirectory;
        DbPath = Path.Join(path, "App_Data", "app.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }
}