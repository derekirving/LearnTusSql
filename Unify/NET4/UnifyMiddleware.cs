#if NETFRAMEWORK
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;

namespace Unify.NET4
{
    public class UnifyWebMiddleware : OwinMiddleware
    {
        public UnifyWebMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public async override Task Invoke(IOwinContext context)
        {
            var httpContextWrapper = context.Environment["System.Web.HttpContextBase"] as HttpContextWrapper;
            var unifyPathBase = Helpers.GetProxyUrl(httpContextWrapper);

            if (!string.IsNullOrEmpty(unifyPathBase))
            {
                context.Request.PathBase = new PathString(unifyPathBase);
            }

            await Next.Invoke(context);
        }
    }
}
#endif
