#if NETFRAMEWORK

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Owin;
using Polly;

namespace Unify.Configuration.NET4
{
    public static class UnifyConfigurationExtensions
    {
        public static IConfigurationRoot UseUnifyConfiguration(this IAppBuilder builder)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetry(25, c => TimeSpan.FromMilliseconds(250));
            
            var remotePath = Environment.GetEnvironmentVariable(Constants.UnifyPathEnvironment);
            
            if (string.IsNullOrEmpty(remotePath))
                remotePath = Environment.GetEnvironmentVariable(Constants.UnifyPathEnvironment, EnvironmentVariableTarget.User);
            
            if (string.IsNullOrEmpty(remotePath))
                throw new ArgumentException("There is no UnifyPath environment variable");

            var configuration = new ConfigurationBuilder()
                .Add(new LegacyConfigurationProvider());
            
            var globalConfigPath = Path.Combine(remotePath, Constants.UnifyGlobalSecretsFile);
            
            DirectoryInfo dirInfo = null;

            policy.Execute(() =>
            {
                dirInfo = new DirectoryInfo(globalConfigPath);
                configuration
                    .AddJsonFile(dirInfo.FullName, optional: false, true);
            });

            if (dirInfo == null)
                throw new Exception("Could not access path on I: drive");

            return configuration.Build();
        }
    }
}
#endif