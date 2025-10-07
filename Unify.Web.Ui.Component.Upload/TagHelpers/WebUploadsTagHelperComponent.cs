using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

public class WebUploadsTagHelperComponent(IMemoryCache cache, IWebHostEnvironment environment) : TagHelperComponent
{
    [ViewContext][HtmlAttributeNotBound] public ViewContext? ViewContext { get; set; }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null)
        {
            output.SuppressOutput();
            return;
        }

        var pathBase = ViewContext.HttpContext.Request.PathBase;
        var isDev = environment.IsDevelopment();

        if (string.Equals(output.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = isDev ? "upload.css" : "upload.min.css";
            var hash = await GetOrCreateHash(fileName);
            var url = $"{pathBase}/unify/uploads/static/{fileName}?v={hash}";
            output.PostContent.AppendHtml($"<link rel=\"stylesheet\" href=\"{url}\">{Environment.NewLine}");
        }
        else if (string.Equals(output.TagName, "body", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = isDev ? "upload.js" : "upload.min.js";
            var hash = await GetOrCreateHash(fileName);
            var url = $"{pathBase}/unify/uploads/static/{fileName}?v={hash}";
            output.PostContent.AppendHtml($"<script src=\"{url}\"></script>{Environment.NewLine}");
        }
    }

    private async Task<string?> GetOrCreateHash(string fileName)
    {
        var identifier = $"Unify.Web.Ui.Component.Upload.Files.{fileName}";

        return await cache.GetOrCreateAsync(identifier + ".hash", async entry =>
        {
            entry.Priority = CacheItemPriority.Low;

            var assembly = typeof(WebUploadsTagHelperComponent).Assembly;
            await using var resourceStream = assembly.GetManifestResourceStream(identifier);
            if (resourceStream == null)
            {
                return string.Empty;
            }

            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(resourceStream);
            return Convert.ToBase64String(hashBytes);
        });
    }
}