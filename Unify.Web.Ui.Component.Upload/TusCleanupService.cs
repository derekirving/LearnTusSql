using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unify.Web.Ui.Component.Upload.Stores;

namespace Unify.Web.Ui.Component.Upload;

public class TusCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TusCleanupService> _logger;

    public TusCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TusCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for 1 minute before starting
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<SharedServerStore>();
                
                // Remove uncommitted files older than 24 hours
                var uncommittedRemoved = await store.CleanupUncommittedFilesAsync(
                    TimeSpan.FromHours(24), 
                    stoppingToken);
                
                if (uncommittedRemoved > 0)
                {
                    _logger.LogInformation($"Removed {uncommittedRemoved} uncommitted files");
                }
                
                // Also remove expired files
                var expiredRemoved = await store.RemoveExpiredFilesAsync(stoppingToken);
                if (expiredRemoved > 0)
                {
                    _logger.LogInformation($"Removed {expiredRemoved} expired files");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TUS cleanup service");
            }

            // Run cleanup every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}