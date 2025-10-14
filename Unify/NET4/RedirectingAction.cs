#if NETFRAMEWORK
using System.Web.Mvc;

namespace Unify.NET4
{
    public class RedirectingAction : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            base.OnResultExecuted(filterContext);
            var result = filterContext.Result as RedirectToRouteResult;
            if (result != null)
            {
                var proxyPathBase = Helpers.GetProxyUrl(filterContext.HttpContext);

                if (!string.IsNullOrEmpty(proxyPathBase))
                    filterContext.HttpContext.Response.RedirectLocation = proxyPathBase + filterContext.HttpContext.Response.RedirectLocation;
            }
        }
    }
}
#endif
