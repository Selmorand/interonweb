using System.Text.Json;
using InteronBlog.Models;
using Microsoft.Extensions.Options;

namespace InteronBlog.Services;

public class SchemaService
{
    private readonly SiteSettings _settings;

    public SchemaService(IOptions<SiteSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateArticleSchema(BlogPost post, string fullUrl)
    {
        var schema = new
        {
            @context = "https://schema.org",
            @graph = new object[]
            {
                // Organization
                new
                {
                    @type = "Organization",
                    @id = $"{_settings.SiteUrl}/#organization",
                    name = _settings.OrganizationName,
                    url = _settings.SiteUrl,
                    logo = new
                    {
                        @type = "ImageObject",
                        url = $"{_settings.SiteUrl}/assets/images/NewLogoWhite 2.png"
                    }
                },
                // WebSite
                new
                {
                    @type = "WebSite",
                    @id = $"{_settings.SiteUrl}/#website",
                    url = _settings.SiteUrl,
                    name = _settings.SiteName,
                    publisher = new { @id = $"{_settings.SiteUrl}/#organization" }
                },
                // WebPage
                new
                {
                    @type = "WebPage",
                    @id = $"{fullUrl}#webpage",
                    url = fullUrl,
                    name = post.GetMetaTitle(),
                    isPartOf = new { @id = $"{_settings.SiteUrl}/#website" },
                    datePublished = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    dateModified = (post.UpdatedAt ?? post.PublishedAt)?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                // Article
                new
                {
                    @type = "Article",
                    @id = $"{fullUrl}#article",
                    isPartOf = new { @id = $"{fullUrl}#webpage" },
                    headline = post.Title,
                    description = post.GetMetaDescription(),
                    datePublished = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    dateModified = (post.UpdatedAt ?? post.PublishedAt)?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    author = new
                    {
                        @type = "Organization",
                        @id = $"{_settings.SiteUrl}/#organization"
                    },
                    publisher = new { @id = $"{_settings.SiteUrl}/#organization" },
                    mainEntityOfPage = new { @id = $"{fullUrl}#webpage" },
                    image = !string.IsNullOrEmpty(post.FeaturedImage)
                        ? (post.FeaturedImage.StartsWith("http") ? post.FeaturedImage : $"{_settings.SiteUrl}{post.FeaturedImage}")
                        : null,
                    articleSection = post.Category,
                    keywords = post.Tags.Any() ? string.Join(", ", post.Tags) : null
                },
                // BreadcrumbList
                new
                {
                    @type = "BreadcrumbList",
                    @id = $"{fullUrl}#breadcrumb",
                    itemListElement = new object[]
                    {
                        new
                        {
                            @type = "ListItem",
                            position = 1,
                            name = "Home",
                            item = _settings.SiteUrl
                        },
                        new
                        {
                            @type = "ListItem",
                            position = 2,
                            name = "Insights",
                            item = $"{_settings.SiteUrl}/insights/"
                        },
                        new
                        {
                            @type = "ListItem",
                            position = 3,
                            name = post.Title,
                            item = fullUrl
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    public string GenerateBlogListSchema(List<BlogPost> posts, string pageUrl, string pageTitle)
    {
        var schema = new
        {
            @context = "https://schema.org",
            @graph = new object[]
            {
                // Organization
                new
                {
                    @type = "Organization",
                    @id = $"{_settings.SiteUrl}/#organization",
                    name = _settings.OrganizationName,
                    url = _settings.SiteUrl
                },
                // WebSite
                new
                {
                    @type = "WebSite",
                    @id = $"{_settings.SiteUrl}/#website",
                    url = _settings.SiteUrl,
                    name = _settings.SiteName,
                    publisher = new { @id = $"{_settings.SiteUrl}/#organization" }
                },
                // CollectionPage
                new
                {
                    @type = "CollectionPage",
                    @id = $"{pageUrl}#webpage",
                    url = pageUrl,
                    name = pageTitle,
                    isPartOf = new { @id = $"{_settings.SiteUrl}/#website" },
                    description = _settings.SiteDescription
                },
                // ItemList
                new
                {
                    @type = "ItemList",
                    mainEntityOfPage = new { @id = $"{pageUrl}#webpage" },
                    numberOfItems = posts.Count,
                    itemListElement = posts.Select((p, i) => new
                    {
                        @type = "ListItem",
                        position = i + 1,
                        url = $"{_settings.SiteUrl}/insights/{p.Slug}.html",
                        name = p.Title
                    }).ToArray()
                },
                // BreadcrumbList
                new
                {
                    @type = "BreadcrumbList",
                    @id = $"{pageUrl}#breadcrumb",
                    itemListElement = new object[]
                    {
                        new
                        {
                            @type = "ListItem",
                            position = 1,
                            name = "Home",
                            item = _settings.SiteUrl
                        },
                        new
                        {
                            @type = "ListItem",
                            position = 2,
                            name = "Insights",
                            item = pageUrl
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    public string GenerateCategorySchema(Category category, List<BlogPost> posts, string pageUrl)
    {
        var schema = new
        {
            @context = "https://schema.org",
            @graph = new object[]
            {
                // Organization
                new
                {
                    @type = "Organization",
                    @id = $"{_settings.SiteUrl}/#organization",
                    name = _settings.OrganizationName,
                    url = _settings.SiteUrl
                },
                // WebSite
                new
                {
                    @type = "WebSite",
                    @id = $"{_settings.SiteUrl}/#website",
                    url = _settings.SiteUrl,
                    name = _settings.SiteName,
                    publisher = new { @id = $"{_settings.SiteUrl}/#organization" }
                },
                // CollectionPage for category
                new
                {
                    @type = "CollectionPage",
                    @id = $"{pageUrl}#webpage",
                    url = pageUrl,
                    name = $"{category.Name} - {_settings.SiteName}",
                    isPartOf = new { @id = $"{_settings.SiteUrl}/#website" },
                    description = category.Description
                },
                // ItemList
                new
                {
                    @type = "ItemList",
                    mainEntityOfPage = new { @id = $"{pageUrl}#webpage" },
                    numberOfItems = posts.Count,
                    itemListElement = posts.Select((p, i) => new
                    {
                        @type = "ListItem",
                        position = i + 1,
                        url = $"{_settings.SiteUrl}/insights/{p.Slug}.html",
                        name = p.Title
                    }).ToArray()
                },
                // BreadcrumbList
                new
                {
                    @type = "BreadcrumbList",
                    @id = $"{pageUrl}#breadcrumb",
                    itemListElement = new object[]
                    {
                        new
                        {
                            @type = "ListItem",
                            position = 1,
                            name = "Home",
                            item = _settings.SiteUrl
                        },
                        new
                        {
                            @type = "ListItem",
                            position = 2,
                            name = "Insights",
                            item = $"{_settings.SiteUrl}/insights/"
                        },
                        new
                        {
                            @type = "ListItem",
                            position = 3,
                            name = category.Name,
                            item = pageUrl
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
