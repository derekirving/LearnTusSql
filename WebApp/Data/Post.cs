using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class Post
{
    public int PostId { get; set; }
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(1000)] public string PostTitle { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
}