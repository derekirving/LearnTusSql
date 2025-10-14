#if NET

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unify.Configuration.FluentOptionsValidation;

namespace Unify.Configuration;

public static class ServiceCollectionExtensions
{
    public static T AddApplicationOptions<T>(this IServiceCollection services, IConfiguration configuration, bool abortStartupOnError = true)
        where T : class, new()
    {
        var type = typeof(T);
        var key = typeof(T).Name;

        services.AddValidatorsFromAssemblyContaining(type);

        services.ConfigureFluentOptions<T>(configuration, c => c.AbortStartupOnError = abortStartupOnError);

        var applicationOptions = new T();
        
        configuration
            .GetSection(key).Bind(applicationOptions);

        return applicationOptions;
    }
}
#endif