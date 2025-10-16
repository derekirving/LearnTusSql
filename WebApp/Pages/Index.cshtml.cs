namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;

public class Index(IUnifyUploads unifyUploads) : PageModel
{
    [BindProperty] public List<UnifyUploadFile> Uploads { get; set; } = [];
    [BindProperty] public string FormSessionId { get; set; }
    
    public void OnGet()
    {
        FormSessionId = unifyUploads.GenerateFormSessionId();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var uploadResults = await unifyUploads.CommitFilesAsync(Uploads, HttpContext.RequestAborted);
        return Page();
    }
}