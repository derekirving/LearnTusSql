namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;

public class Index(IConfiguration configuration, TusApiClient tusApiClient) : PageModel
{
    public string TusApiUrl = configuration["Unify:Uploads:BaseUrl"]!;
    public string? ClientVersion;

    [BindProperty] public List<string> UploadedFileId { get; set; } = [];

    [BindProperty]
    public string FormSessionId { get; set; }
    
    public void OnGet()
    {
        FormSessionId = Guid.NewGuid().ToString("n");
        ClientVersion = tusApiClient.Version;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var uploadResults = await tusApiClient.CommitFilesAsync(UploadedFileId);
        return RedirectToPage();
    }
}