#if NETFRAMEWORK

using System.Configuration;
using Microsoft.Extensions.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace Unify.Configuration.NET4
{
    public class LegacyConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        public override void Load()
        {
            foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
            {
                Data.Add($"ConnectionStrings:{connectionString.Name}", connectionString.ConnectionString);
            }

            foreach (var settingKey in ConfigurationManager.AppSettings.AllKeys)
            {
                Data.Add(settingKey, ConfigurationManager.AppSettings[settingKey]);
            }
        }
        
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }
    }
}
#endif