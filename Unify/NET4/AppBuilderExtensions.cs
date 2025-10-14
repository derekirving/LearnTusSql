#if NETFRAMEWORK
using Owin;
using System.Web.Mvc;
using Unify.Models;

namespace Unify.NET4
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder UseUnify(this IAppBuilder app, UnifyProjectType projectType = UnifyProjectType.Web)
        {
            if (projectType == UnifyProjectType.Web)
            {
                app.Use(typeof(UnifyWebMiddleware));
                GlobalFilters.Filters.Add(new RedirectingAction());
            }
            
            return app;
        }
    }
}
#endif
