#if NET
using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Unify
{
    public static class AppBuilderExtensions
    {
        [Obsolete("Use BuildInfo.Report()")]
        private static IApplicationBuilder UseUnifyVersion(this IApplicationBuilder builder, Assembly assembly, IConfiguration configuration)
        {
            var gitHash = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "GitHash");

            if(gitHash !=null)
            {
                configuration["Unify__GitHash"] = gitHash.Value;
            }
            else
            {
                configuration["Unify__GitHash"] = "No Git Hash";
            }

            return builder;
        }
    }
}
#endif
