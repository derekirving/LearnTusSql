using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;

namespace WebApp.Pages;

public class Index : PageModel
{
    [BindProperty(SupportsGet = true)] public Post? Post { get; set; }
    
    public void OnGet()
    {
        
    }
}