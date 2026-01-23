using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin.Posts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly BlogService _blogService;

    public IndexModel(BlogService blogService)
    {
        _blogService = blogService;
    }

    public List<BlogPost> Posts { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [FromQuery]
    public string? StatusFilter { get; set; }

    [FromQuery]
    public string? CategoryFilter { get; set; }

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _blogService.GetAllCategoriesAsync();
        var allPosts = await _blogService.GetAllPostsAsync(includeUnpublished: true);

        Posts = allPosts;

        // Apply filters
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            Posts = StatusFilter == "published"
                ? Posts.Where(p => p.IsPublished).ToList()
                : Posts.Where(p => !p.IsPublished).ToList();
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            Posts = Posts.Where(p => p.Category.Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var result = await _blogService.DeletePostAsync(id);
        Message = result ? "Post deleted successfully." : "Failed to delete post.";
        return RedirectToPage();
    }
}
