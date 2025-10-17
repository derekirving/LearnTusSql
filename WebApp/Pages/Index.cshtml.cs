namespace WebApp.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unify.Web.Ui.Component.Upload;
using Data;

public class Index(AppDbContext dbContext, IUnifyUploads unifyUploads) : PageModel, IUnifyUploadSession
{
    [BindProperty] public required UnifyUploadSession UploadSession { get; set; }
    [BindProperty] public required Post Post { get; set; }

    public async Task<IActionResult> OnGet(int? postId)
    {
        if (!postId.HasValue)
        {
            Post = new Post();
            UploadSession = new UnifyUploadSession(unifyUploads);
            return Page();
        }

        var post = await dbContext.Posts.FindAsync(postId.Value);
        if (post == null) return NotFound();

        Post = post;
        UploadSession = await unifyUploads.GetSessionAsync(post.UploadId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        await unifyUploads.CommitFilesAsync(UploadSession.Files, HttpContext.RequestAborted);
        Post.UploadId = UploadSession.Id;
        await dbContext.Posts.AddAsync(Post);
        await dbContext.SaveChangesAsync();
        return RedirectToPage("/Index", new { postId = Post.PostId });
    }

    public async Task<IActionResult> OnGetDeleteAsync(string fileId)
    {
        await unifyUploads.DeleteUpload(fileId);
        UploadSession.Files = await unifyUploads.GetFilesBySessionAsync(UploadSession.Id, HttpContext.RequestAborted);
        return Page();
    }
}