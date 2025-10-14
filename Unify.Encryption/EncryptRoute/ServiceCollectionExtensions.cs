using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Unify.Encryption.EncryptRoute;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnifyRouteEncryption(this IServiceCollection services)
    {
        // Original Idea from:
        // https://khalidabuhakmeh.com/how-to-encrypt-aspnet-core-route-parameters
        
        services.Configure<RouteOptions>(opt =>  {
            opt.ConstraintMap.Add("hashid", typeof(HashIdParameter));
        });
        
        services.Configure<RouteOptions>(opt =>  {
            opt.ConstraintMap.Add("encrypt", typeof(EncryptParameter));
        });
        
        return services;
    }
}