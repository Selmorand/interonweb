using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace InteronBlog.Pages.Insights;

public class PostModel : PageModel
{
    private readonly BlogService _blogService;
    private readonly SchemaService _schemaService;
    private readonly SiteSettings _settings;

    public PostModel(BlogService blogService, SchemaService schemaService, IOptions<SiteSettings> settings)
    {
        _blogService = blogService;
        _schemaService = schemaService;
        _settings = settings.Value;
    }

    public BlogPost? Post { get; set; }
    public string? CategoryName { get; set; }
    public List<BlogPost> RelatedPosts { get; set; } = new();
    public string SchemaJson { get; set; } = "";

    public async Task OnGetAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return;
        }

        // Remove .html extension if present
        if (slug.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            slug = slug[..^5];
        }

        Post = await _blogService.GetPostBySlugAsync(slug);

        if (Post == null)
        {
            return;
        }

        // Increment view count
        await _blogService.IncrementViewCountAsync(Post.Id);

        // Get category name
        if (!string.IsNullOrEmpty(Post.Category))
        {
            var categories = await _blogService.GetAllCategoriesAsync();
            var cat = categories.FirstOrDefault(c => c.Slug.Equals(Post.Category, StringComparison.OrdinalIgnoreCase));
            CategoryName = cat?.Name ?? Post.Category;
        }

        // Get related posts (same category, excluding current post)
        if (!string.IsNullOrEmpty(Post.Category))
        {
            var categoryPosts = await _blogService.GetPostsByCategoryAsync(Post.Category);
            RelatedPosts = categoryPosts
                .Where(p => p.Id != Post.Id)
                .Take(3)
                .ToList();
        }

        // If not enough related posts from category, get recent posts
        if (RelatedPosts.Count < 3)
        {
            var recentPosts = await _blogService.GetRecentPostsAsync(5);
            var additionalPosts = recentPosts
                .Where(p => p.Id != Post.Id && !RelatedPosts.Any(r => r.Id == p.Id))
                .Take(3 - RelatedPosts.Count);
            RelatedPosts.AddRange(additionalPosts);
        }

        // Generate schema
        var fullUrl = $"{_settings.SiteUrl}/insights/{Post.Slug}/";
        SchemaJson = _schemaService.GenerateArticleSchema(Post, fullUrl);
    }
}
