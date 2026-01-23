namespace InteronBlog.Models;

public class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public int SortOrder { get; set; } = 0;

    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        var slug = name.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}

// Predefined categories as per the plan
public static class DefaultCategories
{
    public static List<Category> GetAll() => new()
    {
        new Category { Id = "ai-readiness", Name = "AI Readiness", Slug = "ai-readiness", Description = "Understanding and improving your business's AI discoverability", SortOrder = 1 },
        new Category { Id = "schema-markup", Name = "Schema Markup", Slug = "schema-markup", Description = "Implementing structured data for better machine understanding", SortOrder = 2 },
        new Category { Id = "seo", Name = "SEO", Slug = "seo", Description = "Search engine optimization strategies and best practices", SortOrder = 3 },
        new Category { Id = "geo", Name = "GEO", Slug = "geo", Description = "Generative Engine Optimization for AI-powered search", SortOrder = 4 },
        new Category { Id = "case-studies", Name = "Case Studies", Slug = "case-studies", Description = "Real-world examples and success stories", SortOrder = 5 }
    };
}
