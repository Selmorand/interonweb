using System.Text.Json;
using InteronBlog.Models;

namespace InteronBlog.Services;

public class BlogService
{
    private readonly string _postsFilePath;
    private readonly string _categoriesFilePath;
    private readonly ILogger<BlogService> _logger;
    private readonly object _lock = new();
    private List<BlogPost>? _postsCache;
    private List<Category>? _categoriesCache;
    private DateTime _postsCacheTime = DateTime.MinValue;
    private DateTime _categoriesCacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BlogService(IWebHostEnvironment env, ILogger<BlogService> logger)
    {
        _logger = logger;
        var dataPath = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);

        _postsFilePath = Path.Combine(dataPath, "posts.json");
        _categoriesFilePath = Path.Combine(dataPath, "categories.json");

        // Initialize files if they don't exist
        InitializeDataFiles();
    }

    private void InitializeDataFiles()
    {
        if (!File.Exists(_postsFilePath))
        {
            File.WriteAllText(_postsFilePath, JsonSerializer.Serialize(new List<BlogPost>(), JsonOptions));
        }

        if (!File.Exists(_categoriesFilePath))
        {
            var defaultCategories = DefaultCategories.GetAll();
            File.WriteAllText(_categoriesFilePath, JsonSerializer.Serialize(defaultCategories, JsonOptions));
        }
    }

    // Posts CRUD operations
    public async Task<List<BlogPost>> GetAllPostsAsync(bool includeUnpublished = false)
    {
        var posts = await LoadPostsAsync();
        if (!includeUnpublished)
        {
            posts = posts.Where(p => p.IsPublished).ToList();
        }
        return posts.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt).ToList();
    }

    public async Task<List<BlogPostListItem>> GetPostListAsync(bool includeUnpublished = false)
    {
        var posts = await GetAllPostsAsync(includeUnpublished);
        return posts.Select(BlogPostListItem.FromPost).ToList();
    }

    public async Task<BlogPost?> GetPostByIdAsync(string id)
    {
        var posts = await LoadPostsAsync();
        return posts.FirstOrDefault(p => p.Id == id);
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var posts = await LoadPostsAsync();
        return posts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && p.IsPublished);
    }

    public async Task<List<BlogPost>> GetPostsByCategoryAsync(string categorySlug, bool includeUnpublished = false)
    {
        var posts = await GetAllPostsAsync(includeUnpublished);
        return posts.Where(p => !string.IsNullOrEmpty(p.Category) && p.Category.Equals(categorySlug, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<BlogPost>> GetPostsByTagAsync(string tag, bool includeUnpublished = false)
    {
        var posts = await GetAllPostsAsync(includeUnpublished);
        return posts.Where(p => p.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    public async Task<List<BlogPost>> GetFeaturedPostsAsync(int count = 3)
    {
        var posts = await GetAllPostsAsync(false);
        return posts.Where(p => p.IsFeatured).Take(count).ToList();
    }

    public async Task<List<BlogPost>> GetRecentPostsAsync(int count = 5)
    {
        var posts = await GetAllPostsAsync(false);
        return posts.Take(count).ToList();
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        var posts = await LoadPostsAsync();

        // Generate slug if empty
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = BlogPost.GenerateSlug(post.Title);
        }

        // Ensure unique slug
        post.Slug = EnsureUniqueSlug(post.Slug, posts, post.Id);

        // Set timestamps
        post.CreatedAt = DateTime.UtcNow;
        if (post.IsPublished && !post.PublishedAt.HasValue)
        {
            post.PublishedAt = DateTime.UtcNow;
        }

        posts.Add(post);
        await SavePostsAsync(posts);

        _logger.LogInformation("Created blog post: {Title} ({Id})", post.Title, post.Id);
        return post;
    }

    public async Task<BlogPost?> UpdatePostAsync(BlogPost post)
    {
        var posts = await LoadPostsAsync();
        var existingIndex = posts.FindIndex(p => p.Id == post.Id);

        if (existingIndex < 0)
            return null;

        var existing = posts[existingIndex];

        // Generate slug if changed
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = BlogPost.GenerateSlug(post.Title);
        }

        // Ensure unique slug
        post.Slug = EnsureUniqueSlug(post.Slug, posts, post.Id);

        // Update timestamps
        post.CreatedAt = existing.CreatedAt;
        post.UpdatedAt = DateTime.UtcNow;

        // Handle publishedAt logic
        if (post.IsPublished && !existing.IsPublished)
        {
            // Publishing for the first time
            post.PublishedAt = DateTime.UtcNow;
        }
        else if (post.IsPublished && existing.IsPublished)
        {
            // Already published, preserve existing date
            post.PublishedAt = existing.PublishedAt;
        }
        else if (!post.IsPublished)
        {
            // Unpublishing, clear the date
            post.PublishedAt = null;
        }

        posts[existingIndex] = post;
        await SavePostsAsync(posts);

        _logger.LogInformation("Updated blog post: {Title} ({Id})", post.Title, post.Id);
        return post;
    }

    public async Task<bool> DeletePostAsync(string id)
    {
        var posts = await LoadPostsAsync();
        var post = posts.FirstOrDefault(p => p.Id == id);

        if (post == null)
            return false;

        posts.Remove(post);
        await SavePostsAsync(posts);

        _logger.LogInformation("Deleted blog post: {Title} ({Id})", post.Title, post.Id);
        return true;
    }

    public async Task IncrementViewCountAsync(string id)
    {
        var posts = await LoadPostsAsync();
        var post = posts.FirstOrDefault(p => p.Id == id);

        if (post != null)
        {
            post.ViewCount++;
            await SavePostsAsync(posts);
        }
    }

    // Categories operations
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await LoadCategoriesAsync();
    }

    public async Task<Category?> GetCategoryBySlugAsync(string slug)
    {
        var categories = await LoadCategoriesAsync();
        return categories.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    // Statistics
    public async Task<BlogStats> GetStatsAsync()
    {
        var posts = await LoadPostsAsync();
        return new BlogStats
        {
            TotalPosts = posts.Count,
            PublishedPosts = posts.Count(p => p.IsPublished),
            DraftPosts = posts.Count(p => !p.IsPublished),
            TotalViews = posts.Sum(p => p.ViewCount),
            PostsByCategory = posts
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    // Private helper methods
    private string EnsureUniqueSlug(string slug, List<BlogPost> posts, string currentId)
    {
        var baseSlug = slug;
        var counter = 1;

        while (posts.Any(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && p.Id != currentId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private async Task<List<BlogPost>> LoadPostsAsync()
    {
        lock (_lock)
        {
            if (_postsCache != null && DateTime.UtcNow - _postsCacheTime < CacheDuration)
            {
                return new List<BlogPost>(_postsCache);
            }
        }

        try
        {
            var json = await File.ReadAllTextAsync(_postsFilePath);
            var posts = JsonSerializer.Deserialize<List<BlogPost>>(json, JsonOptions) ?? new List<BlogPost>();

            lock (_lock)
            {
                _postsCache = posts;
                _postsCacheTime = DateTime.UtcNow;
            }

            return new List<BlogPost>(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading posts from {Path}", _postsFilePath);
            return new List<BlogPost>();
        }
    }

    private async Task SavePostsAsync(List<BlogPost> posts)
    {
        lock (_lock)
        {
            _postsCache = null;
        }

        var json = JsonSerializer.Serialize(posts, JsonOptions);
        await File.WriteAllTextAsync(_postsFilePath, json);
    }

    private async Task<List<Category>> LoadCategoriesAsync()
    {
        lock (_lock)
        {
            if (_categoriesCache != null && DateTime.UtcNow - _categoriesCacheTime < CacheDuration)
            {
                return new List<Category>(_categoriesCache);
            }
        }

        try
        {
            var json = await File.ReadAllTextAsync(_categoriesFilePath);
            var categories = JsonSerializer.Deserialize<List<Category>>(json, JsonOptions) ?? new List<Category>();

            lock (_lock)
            {
                _categoriesCache = categories;
                _categoriesCacheTime = DateTime.UtcNow;
            }

            return new List<Category>(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories from {Path}", _categoriesFilePath);
            return DefaultCategories.GetAll();
        }
    }
}

public class BlogStats
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int TotalViews { get; set; }
    public Dictionary<string, int> PostsByCategory { get; set; } = new();
}
