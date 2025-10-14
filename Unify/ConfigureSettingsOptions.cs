#if NET
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unify;

internal class ConfigureSettingsOptions<T> : IPostConfigureOptions<T> where T : UnifyBaseConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<T> _logger;

    public ConfigureSettingsOptions(IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger<T> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public void PostConfigure(string name, T options)
    {
        if (!string.IsNullOrEmpty(_configuration[Constants.UnifyAppBaseUrl]))
        {
            return;
        }
       
        _logger.LogDebug("Running PostConfigure for {Name}", typeof(T).Name);
        
        options.UnifyConfigLoaded = DateTime.Parse(DateTime.Now.ToString("f"), new CultureInfo(options.DefaultCulture));

        options.BasePath = _hostEnvironment.ContentRootPath;

        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null) return;
        var gitHash = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => attr.Key == "GitHash");

        if(gitHash != null)
        {
            options.GitHash = gitHash.Value;
        }
    }
}

#endif