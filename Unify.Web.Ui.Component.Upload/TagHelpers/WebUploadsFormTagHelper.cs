using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("form", Attributes = "asp-for")]
public class CustomFormTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")] public required ModelExpression For { get; set; }
    
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var hiddenInput = $"<input class='unify-form-id' type=\"hidden\" name=\"{For.Name}\" value=\"{For.Model}\" />";
        output.PostContent.AppendHtml(hiddenInput);
    }
}
