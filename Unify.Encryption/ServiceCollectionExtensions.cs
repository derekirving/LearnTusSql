using Microsoft.Extensions.DependencyInjection;

namespace Unify.Encryption;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnifyEncryption(this IServiceCollection services)
    {
        services.AddSingleton<IUnifyEncryption, UnifyEncryption>();
        return services;
    }
}