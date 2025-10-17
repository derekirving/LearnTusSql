using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class Post
{
    public int PostId { get; init; }
    
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(1000)] 
    public string PostTitle { get; init; } = string.Empty;
    
    [MaxLength(32)] 
    public string UploadId { get; set; } = string.Empty;
}