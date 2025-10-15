using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

[HtmlTargetElement("unify-web-upload")]
public class WebUploadsTagHelper(IOptions<UnifyUploadOptions> uploadOptions, UnifyUploadService uploadService, TusApiClient client) : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var v = TusApiClient.Version.ToString();
        var min = uploadService.GetMinimumFiles("MyUploadZone");
        
        output.TagName = "";
        output.Content.SetHtmlContent($"<p>AppId: {uploadOptions.Value.EncryptedAppId}</p>");

        await base.ProcessAsync(context, output);
    }
}