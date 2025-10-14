using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class SimpleTagHelperComponent(IOptions<UnifyUploadOptions> options) : TagHelperComponent
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.Equals(output.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
            var encryptedId = options.Value.EncryptedAppId;
            output.PostContent.AppendHtml($"<meta name=\"unify-upload-id\" content==\"{encryptedId}\" />{Environment.NewLine}");
        }
        
        base.Process(context, output);
    }
}