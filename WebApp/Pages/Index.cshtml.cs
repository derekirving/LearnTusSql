namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;

public class Index(IConfiguration configuration, IUnifyUploads unifyUploads) : PageModel
{
    public string TusApiUrl = configuration["Unify:Uploads:BaseUrl"]!;
    public string? ClientVersion;

    [BindProperty] public List<string> UploadedFileId { get; set; } = [];

    [BindProperty]
    public string FormSessionId { get; set; } // WIIL BE DONE IN TAG-HELPER
    
    public void OnGet()
    {
        FormSessionId = Guid.NewGuid().ToString("n");
        ClientVersion = unifyUploads.ClientVersion();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var uploadResults = await unifyUploads.CommitFilesAsync(UploadedFileId, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}