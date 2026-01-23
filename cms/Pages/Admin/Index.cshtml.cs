using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin;

[Authorize]
public class IndexModel : PageModel
{
    private readonly BlogService _blogService;

    public IndexModel(BlogService blogService)
    {
        _blogService = blogService;
    }

    public BlogStats Stats { get; set; } = new();
    public List<BlogPost> RecentPosts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Stats = await _blogService.GetStatsAsync();
        var allPosts = await _blogService.GetAllPostsAsync(includeUnpublished: true);
        RecentPosts = allPosts.Take(5).ToList();
    }
}
