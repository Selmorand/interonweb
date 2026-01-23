namespace InteronBlog.Models;

public class BlogPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Content { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string FeaturedImage { get; set; } = "";
    public string FeaturedImageAlt { get; set; } = "";
    public string MetaTitle { get; set; } = "";
    public string MetaDescription { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Author { get; set; } = "Interon";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    // Computed properties
    public string GetMetaTitle() => string.IsNullOrEmpty(MetaTitle) ? Title : MetaTitle;

    public string GetMetaDescription() => string.IsNullOrEmpty(MetaDescription)
        ? (Excerpt.Length > 160 ? Excerpt.Substring(0, 157) + "..." : Excerpt)
        : MetaDescription;

    public string GetExcerpt(int maxLength = 200)
    {
        if (!string.IsNullOrEmpty(Excerpt))
            return Excerpt.Length > maxLength ? Excerpt.Substring(0, maxLength - 3) + "..." : Excerpt;

        // Strip HTML and get excerpt from content
        var text = System.Text.RegularExpressions.Regex.Replace(Content, "<[^>]*>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length > maxLength ? text.Substring(0, maxLength - 3) + "..." : text;
    }

    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrEmpty(title))
            return "";

        var slug = title.ToLowerInvariant();
        // Replace spaces with hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        // Remove invalid characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        // Remove multiple hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        // Trim hyphens from ends
        slug = slug.Trim('-');

        return slug;
    }
}

public class BlogPostListItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string FeaturedImage { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    public static BlogPostListItem FromPost(BlogPost post)
    {
        return new BlogPostListItem
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.GetExcerpt(),
            FeaturedImage = post.FeaturedImage,
            Category = post.Category,
            Tags = post.Tags,
            PublishedAt = post.PublishedAt,
            IsPublished = post.IsPublished,
            IsFeatured = post.IsFeatured
        };
    }
}
