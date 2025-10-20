using Microsoft.AspNetCore.Http;
using tusdotnet.Models;

namespace Unify.Web.Ui.Component.Upload.Interfaces;

    public interface ITusConfigurationFactory
    {
        DefaultTusConfiguration Create(HttpContext httpContext);
    }
