using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

[HtmlTargetElement("unify-web-upload-simple")]
public class SimpleTagHelper(IConfiguration configuration) : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        
    }
}