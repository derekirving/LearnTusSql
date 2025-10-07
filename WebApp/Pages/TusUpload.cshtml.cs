using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;
using WebApp.Data;

public class TusUpload(IConfiguration configuration, TusApiClient tusApiClient) : PageModel
{
    public string TusApiUrl = "http://localhost:5289";
    public string AppId = configuration["Unify:Uploads:AppId"]!;
    
    [BindProperty]
    public string UploadedFileId { get; set; }

    [BindProperty]
    public string FormSessionId { get; set; }
    
    public void OnGet()
    {
        FormSessionId = Guid.NewGuid().ToString("n");
        
        var v = tusApiClient.Version;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var committed = await tusApiClient.CommitFileAsync(UploadedFileId);
        return RedirectToPage("Success");
    }
}