using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Unify.Web.Ui.Component.Upload.Constants;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("form", Attributes = "submit-after-unify-uploads")]

public class WebUploadsFormTagHelper : TagHelper
{
    [ViewContext] [HtmlAttributeNotBound] public ViewContext? ViewContext { get; set; }
    [HtmlAttributeName("submit-after-unify-uploads")] public bool AutoSubmit { get; set; }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var model = ViewContext?.ViewData.Model;
        ArgumentNullException.ThrowIfNull(model);
        
        var sessionProperty = PropertyInfoCache.GetPropertyInfo(model.GetType(), nameof(IUnifyUploadSession.UploadSession));
        var session = sessionProperty?.GetValue(model);
        ArgumentNullException.ThrowIfNull(session);

        var idProperty = PropertyInfoCache.GetPropertyInfo(session.GetType(), nameof(UnifyUploadSession.Id));
        var value = idProperty?.GetValue(session);
        ArgumentNullException.ThrowIfNull(value);

        if (AutoSubmit)
        {
            const string customClass = "submit-after-unify-uploads";
            
            var classAttr = output.Attributes["class"];
            var existingClasses = classAttr?.Value?.ToString() ?? string.Empty;
            
            var newClasses = string.IsNullOrWhiteSpace(existingClasses)
                ? customClass
                : $"{existingClasses} {customClass}";
            
            output.Attributes.SetAttribute("class", newClasses);
        }
        
        const string name = $"{nameof(IUnifyUploadSession.UploadSession)}.{nameof(UnifyUploadSession.Id)}";
        var hiddenInput = $"<input class=\"unify-upload-session-id\" type=\"hidden\" name=\"{name}\" value=\"{value}\" />";
        output.PostContent.AppendHtml(hiddenInput);
    }
}