using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using tusdotnet.Models;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

[HtmlTargetElement("unify-web-upload-htmx")]
public class WebUploadsHtmxTagHelper(IUnifyUploads uploads, LinkGenerator linkGenerator) : TagHelper
{
    public required string Zone { get; set; }
    public List<string>? Files { get; set; }

    [HtmlAttributeName("hx-post")] public string? Page { get; set; }

    [HtmlAttributeName("hx-delete")]
    public string? DeleteHandler { get; set; }

    [HtmlAttributeName(DictionaryAttributePrefix = "asp-route-")]
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    [ViewContext] [HtmlAttributeNotBound] public ViewContext? ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null)
        {
            output.SuppressOutput();
            return;
        }

        var maxFiles = uploads.GetMaximumFiles(Zone);
        var maxSize = uploads.GetMaximumFileSize(Zone);

        if (maxFiles == 0 || maxSize == 0)
        {
            throw new UploadException($"MaxSize or MaxFiles for zone {Zone} has not been set");
        }

        var getAccepted = uploads.GetAcceptedFileTypes(Zone);
        var accepted = string.Join(",", getAccepted);
        
        var minFiles = uploads.GetMinimumFiles(Zone);

        
        var maxSizeDisplay = Math.Floor(uploads.GetMaximumFileSize(Zone) / 1024.0 * 10) / 10;

        var pathBase = ViewContext.HttpContext.Request.PathBase.Value;

        var href = string.Empty;
        var deleteLink = linkGenerator.GetUriByPage(ViewContext.HttpContext, Page, DeleteHandler);

        if (!string.IsNullOrEmpty(deleteLink))
        {
            var uri = new Uri(deleteLink);
            href = uri.PathAndQuery;
            
            href = QueryHelpers.AddQueryString(href, "zone", Zone);

            if (RouteValues.Count != 0) href = QueryHelpers.AddQueryString(href, RouteValues);
        }

        var className = output.Attributes.FirstOrDefault(a => a.Name == "class")?.Value?.ToString();
        var useClass = $"zone{(string.IsNullOrEmpty(className) ? "" : $" {className}")}";

        var isSingle = getAccepted.Count == 1;
        var isMultiple = maxFiles > 1;

        var textChoose = isMultiple ? "Choose files" : "Choose a file";
        var textDrag = isMultiple ? "or drag them here" : "or drag it here";
        var textLabel = isMultiple
            ? $"You can upload a maximum of {maxFiles} \"{accepted}\" files.<br/>Each file can be up to {maxSizeDisplay}KB in size."
            : $"The file extension {(isSingle ? "must be" : "can be any of")} \"{accepted}\".<br/>The file can be a maximum size of {maxSizeDisplay}KB.";


        var successMessage = "Uploads done. Submitting form...";
        var childContent = (await output.GetChildContentAsync()).GetContent();
        if (!string.IsNullOrEmpty(childContent))
        {
            successMessage = childContent;
        }
        
        var fileListHtml = new StringBuilder();
        if (Files != null && Files.Count != 0)
        {
            fileListHtml.AppendLine("<ul>");
            foreach (var file in Files)
            {
                href = QueryHelpers.AddQueryString(href, "fileId", file);
                var meta = await uploads.GetFileInfo(file);
                fileListHtml.AppendLine($"""
                                                             <li>
                                                                 <span>
                                                                     <a title="Open this file" class="text-decoration-none" href="{pathBase}/unify/uploads/{file}">
                                                                         <svg class="mb-1" width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                                                           <path d="M4 2H14L20 8V20C20 21.1 19.1 22 18 22H6C4.9 22 4 21.1 4 20V2Z" stroke="gray" stroke-width="2" stroke-linejoin="round"/>
                                                                           <path d="M14 2V8H20" stroke="gray" stroke-width="2" stroke-linejoin="round"/>
                                                                         </svg>
                                                                         {meta?.FileName}
                                                                     </a> 
                                                                     <small class="text-muted">({meta?.Size / 1024.0:0.0} KB)</small>
                                                                 </span>
                                                                 <a data-file-name="{meta?.FileName}" class="zone__remove-file" title="Delete this file" aria-label="Remove File" hx-get="{href}" href="{href}">
                                                                     <svg width="16" height="16" viewBox="0 0 16 16" fill="none"
                                                                          xmlns="http://www.w3.org/2000/svg">
                                                                         <line x1="4" y1="4" x2="12" y2="12" stroke="red" stroke-width="2"/>
                                                                         <line x1="12" y1="4" x2="4" y2="12" stroke="red" stroke-width="2"/>
                                                                     </svg>
                                                                 </a>
                                                             </li>
                                                         
                                         """);
            }

            fileListHtml.AppendLine("</ul>");
        }

        var html = $$"""
                             <div class="{{useClass}}" data-zone="{{Zone}}" data-accepted="{{accepted}}" data-max-files="{{maxFiles}}" data-min-files="{{minFiles}}" data-max-file-size="{{maxSize}}" data-file-count="{{Files?.Count ?? 0}}">
                                 <div class="zone__input">
                                     <svg class="zone__icon" xmlns="http://www.w3.org/2000/svg" width="50" height="43" viewBox="0 0 50 43">
                                         <path d="M48.4 26.5c-.9 0-1.7.7-1.7 1.7v11.6h-43.3v-11.6c0-.9-.7-1.7-1.7-1.7s-1.7.7-1.7 1.7v13.2c0 .9.7 1.7 1.7 1.7h46.7c.9 0 1.7-.7 1.7-1.7v-13.2c0-1-.7-1.7-1.7-1.7zm-24.5 6.1c.3.3.8.5 1.2.5.4 0 .9-.2 1.2-.5l10-11.6c.7-.7.7-1.7 0-2.4s-1.7-.7-2.4 0l-7.1 8.3v-25.3c0-.9-.7-1.7-1.7-1.7s-1.7.7-1.7 1.7v25.3l-7.1-8.3c-.7-.7-1.7-.7-2.4 0s-.7 1.7 0 2.4l10 11.6z"/>
                                     </svg>
                                     <div class="fileList">{{fileListHtml}}</div>
                                     <input type="file" name="files[]" id="file_{{Zone}}" class="zone__file" data-multiple-caption="{count} files selected" multiple />
                                     <label for="file_{{Zone}}"><strong>{{textChoose}}</strong><span class="zone__dragndrop"> {{textDrag}}</span><br/>{{textLabel}}</label>
                                 </div>
                                 <div class="zone__uploading">
                                      <div class="container">
                                         <div class="progress" role="progressbar" aria-label="Example with label" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                                           <div class="progress-bar text-bg-success progress-bar-striped progress-bar-animated" style="width: 0%"></div>
                                         </div>
                                       </div>
                                 </div>
                                 <div class="zone__success text-center">{{successMessage}}</div>
                                 <div class="zone__error text-center">Error! <span></span>. <a href="#" class="zone__restart" role="button">Try again!</a></div>
                             </div>
                     """;

        output.TagName = "";
        output.Content.SetHtmlContent(html);
    }
}