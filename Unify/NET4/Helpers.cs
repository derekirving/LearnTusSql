#if NETFRAMEWORK
using System.Web;

namespace Unify.NET4
{
    public static class Helpers
    {
        public static string GetProxyUrl(HttpContextBase context)
        {
            var proxyPathBase = context.Request.Headers[Constants.UnifyForwardPathBase];

            if (string.IsNullOrEmpty(proxyPathBase))
            {
                proxyPathBase = context.Request.ServerVariables[Constants.UnifyHttpForwardPathBase];
            }

            return proxyPathBase;
        }

        public static string GetProxyUrl(HttpContext context)
        {
            var proxyPathBase = context.Request.Headers[Constants.UnifyForwardPathBase];

            if (string.IsNullOrEmpty(proxyPathBase))
            {
                proxyPathBase = context.Request.ServerVariables[Constants.UnifyHttpForwardPathBase];
            }

            return proxyPathBase;
        }
    }
}
#endif
