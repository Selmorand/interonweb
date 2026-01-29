using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin;

[Authorize]
public class KnowledgeGraphBuilderModel : PageModel
{
    public void OnGet()
    {
    }
}
