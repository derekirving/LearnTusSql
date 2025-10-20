#if DEBUG
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tusdotnet.Models;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Stores;

namespace Unify.Web.Ui.Component.Upload;

public class TusConfigurationFactoryDev(ILogger<TusConfigurationFactoryDev> logger) : ITusConfigurationFactory
{
    public DefaultTusConfiguration Create(HttpContext httpContext)
    {
        var store = httpContext.RequestServices.GetRequiredService<TusSqliteStore>();
        return new DefaultTusConfiguration
        {
            Store = store,
            UrlPath = "/unify/uploads",
        };
    }
}
#endif