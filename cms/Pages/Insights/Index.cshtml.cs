using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace InteronBlog.Pages.Insights;

public class IndexModel : PageModel
{
    private readonly BlogService _blogService;
    private readonly SchemaService _schemaService;
    private readonly SiteSettings _settings;
    private const int PageSize = 12;

    public IndexModel(BlogService blogService, SchemaService schemaService, IOptions<SiteSettings> settings)
    {
        _blogService = blogService;
        _schemaService = schemaService;
        _settings = settings.Value;
    }

    public List<BlogPost> Posts { get; set; } = new();
    public List<BlogPost> FeaturedPosts { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? CurrentCategory { get; set; }
    public string? CurrentCategoryName { get; set; }
    public string? CurrentCategoryDescription { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string SchemaJson { get; set; } = "";

    public async Task OnGetAsync([FromQuery] string? category, [FromQuery] int page = 1)
    {
        Categories = await _blogService.GetAllCategoriesAsync();
        CurrentCategory = category;
        CurrentPage = Math.Max(1, page);

        // Get posts
        List<BlogPost> allPosts;
        if (!string.IsNullOrEmpty(category))
        {
            allPosts = await _blogService.GetPostsByCategoryAsync(category);
            var cat = Categories.FirstOrDefault(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase));
            CurrentCategoryName = cat?.Name ?? category;
            CurrentCategoryDescription = cat?.Description;
        }
        else
        {
            allPosts = await _blogService.GetAllPostsAsync();
            FeaturedPosts = allPosts.Where(p => p.IsFeatured).Take(2).ToList();
            // Remove featured posts from main list to avoid duplication
            allPosts = allPosts.Where(p => !p.IsFeatured || FeaturedPosts.Count == 0).ToList();
        }

        // Pagination
        TotalPages = (int)Math.Ceiling(allPosts.Count / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        Posts = allPosts
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Generate schema
        var pageUrl = $"{_settings.SiteUrl}/insights/";
        if (!string.IsNullOrEmpty(category))
        {
            var cat = Categories.FirstOrDefault(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (cat != null)
            {
                SchemaJson = _schemaService.GenerateCategorySchema(cat, allPosts, pageUrl + "?category=" + category);
            }
        }
        else
        {
            SchemaJson = _schemaService.GenerateBlogListSchema(allPosts, pageUrl, "Insights - " + _settings.SiteName);
        }
    }
}
