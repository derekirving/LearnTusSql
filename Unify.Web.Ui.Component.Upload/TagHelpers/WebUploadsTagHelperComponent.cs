using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class WebUploadsTagHelperComponent(IOptions<UnifyUploadOptions> options, IMemoryCache cache) : TagHelperComponent
{
    [ViewContext] [HtmlAttributeNotBound] public ViewContext? ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null)
        {
            return;
        }
        
        if (string.Equals(output.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
#if DEBUG
            output.PostContent.AppendHtml("<!-- Unify Uploads running in DEBUG mode -->" + Environment.NewLine);
#endif
            var uploadBaseUrl = options.Value.BaseUrl;
            output.PostContent.AppendHtml(
                $"{Environment.NewLine}\t<meta name=\"unify-upload-baseUrl\" content=\"{uploadBaseUrl}\" />");

            var postContentString = output.PostContent.GetContent();
            if (!postContentString.Contains($"name=\"{UploadConstants.UnifyAppId}\"",
                    StringComparison.OrdinalIgnoreCase))
            {
                var encryptedId = options.Value.EncryptedAppId;
                output.PostContent.AppendHtml(
                    $"{Environment.NewLine}\t<meta name=\"{UploadConstants.UnifyAppId}\" content=\"{encryptedId}\">{Environment.NewLine}");
            }

            var hash = await CalculateFileHashAsync("Files.upload.css");

            output.PostContent
                .AppendHtml(
                    $"<link id=\"{UploadConstants.NameSpace}.Css\" rel=\"stylesheet\" href=\"{ViewContext.HttpContext.Request.PathBase}/unify/uploads/static/style.css?v={hash}\">{Environment.NewLine}");
        }

        if (string.Equals(output.TagName, "body", StringComparison.OrdinalIgnoreCase))
        {
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
            
            var hash = await CalculateFileHashAsync("Files.upload.js");

            output.PostContent
                .AppendHtml(
                    $"<script id=\"{UploadConstants.NameSpace}.Js\" src=\"{ViewContext.HttpContext.Request.PathBase}/unify/uploads/static/script.js?v={hash}\"></script>{Environment.NewLine}");
        }

        await base.ProcessAsync(context, output);
    }
    
    // TODO: This should be included in Unify.Web as a similar thing is used by Unify.Messaging.WebPush
    private async Task<string?> CalculateFileHashAsync(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var key = $"{UploadConstants.NameSpace}{extension}_hash";

        var hash = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.Priority = CacheItemPriority.Low;
            var assembly = typeof(UnifyUploads).Assembly;
            var resourceName = $"{UploadConstants.NameSpace}.{fileName}";
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null) return string.Empty;
            entry.Priority = CacheItemPriority.NeverRemove;
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(resourceStream);
            return Convert.ToBase64String(hashBytes);
        });

        return hash;
    }
}