using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Unify.Validation.Binary;

namespace Unify.Validation
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUnifyBinaryValidation(this IServiceCollection services, IEnumerable<string> types)
        {
            services.AddSingleton<IUnifyBinaryValidator>(_ => new UnifyBinaryValidator(types));
            return services;
        }
    }
}
