using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class WebUploadsTagHelperComponent(IOptions<UnifyUploadOptions> options) : TagHelperComponent
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
       AddNoJs(output);

        if (string.Equals(output.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
            var encryptedId = options.Value.EncryptedAppId;
            output.PostContent.AppendHtml($"<meta name=\"unify-upload-id\" content==\"{encryptedId}\" />{Environment.NewLine}");
        }
        
        await base.ProcessAsync(context, output);
    }

    private static void AddNoJs(TagHelperOutput output)
    {
        if (!string.Equals(output.TagName, "body", StringComparison.OrdinalIgnoreCase)) return;
        const string classToAdd = "no-js";
        if (output.Attributes.TryGetAttribute("class", out var classAttr))
        {
            var existing = classAttr.Value?.ToString();
            var newValue = string.IsNullOrWhiteSpace(existing) ? classToAdd : $"{existing} {classToAdd}";
            output.Attributes.SetAttribute("class", newValue);
        }
        else
        {
            output.Attributes.Add("class", classToAdd);
        }
    }
}