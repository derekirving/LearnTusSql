using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class WebUploadsTagHelperInitialiser(string version) : ITagHelperInitializer<WebUploadsTagHelper>
{
    public void Initialize(WebUploadsTagHelper helper, ViewContext context)
    {
        helper.Version = version;
    }
}