using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class WebUploadsTagHelperComponent(IOptions<UnifyUploadOptions> options) : TagHelperComponent
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
       AddNoJs(output);

        if (string.Equals(output.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
#if DEBUG
            output.PostContent.AppendHtml("<!-- Unify Uploads running in DEBUG mode -->" + Environment.NewLine);
#endif
            var baseUrl = options.Value.BaseUrl;
            output.PostContent.AppendHtml($"{Environment.NewLine}\t<meta name=\"unify-upload-baseUrl\" content=\"{baseUrl}\" />");
            
            //var encryptedId = options.Value.EncryptedAppId;
            //output.PostContent.AppendHtml($"<meta name=\"unify-upload-id\" content=\"{encryptedId}\" />{Environment.NewLine}");
            
            var postContentString = output.PostContent.GetContent();
            if (!postContentString.Contains($"name=\"{UploadConstants.UnifyAppId}\"", StringComparison.OrdinalIgnoreCase))
            {
                var encryptedId = options.Value.EncryptedAppId;
                output.PostContent.AppendHtml($"{Environment.NewLine}\t<meta name=\"{UploadConstants.UnifyAppId}\" content=\"{encryptedId}\">{Environment.NewLine}");
            }
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