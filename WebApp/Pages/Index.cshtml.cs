using WebApp.Data;

namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;

public class Index(AppDbContext dbContext, IUnifyUploads unifyUploads) : PageModel, IUnifyUploadSession
{
    [BindProperty] public required string UnifyUploadId { get; set; }
    [BindProperty] public List<UnifyUploadFile> UnifyUploads { get; set; } = [];

    [BindProperty] public Post Post { get; set; } = new();
    public async Task<IActionResult> OnGet(int? postId)
    {
        if (!postId.HasValue)
        {
            UnifyUploadId = unifyUploads.GenerateUploadId();
            return Page();
        }
        
        var post = await dbContext.Posts.FindAsync(postId.Value);
        if (post == null) return NotFound();

        UnifyUploadId = post.UploadId;
        UnifyUploads = await unifyUploads.GetFilesBySessionAsync(UnifyUploadId, HttpContext.RequestAborted);
        
        Post = post;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var uploadResults = await unifyUploads.CommitFilesAsync(UnifyUploads, HttpContext.RequestAborted);
        Post.UploadId = UnifyUploadId;
        await dbContext.Posts.AddAsync(Post);
        await dbContext.SaveChangesAsync();
        return RedirectToPage("/Index",  new { postId = Post.PostId });
    }

    public async Task<IActionResult> OnGetDelete(string fileId)
    {
        await unifyUploads.DeleteUpload(fileId);
        UnifyUploads = await unifyUploads.GetFilesBySessionAsync(UnifyUploadId);

        return Page();
    }
}