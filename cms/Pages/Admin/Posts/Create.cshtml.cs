using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin.Posts;

[Authorize]
public class CreateModel : PageModel
{
    private readonly BlogService _blogService;
    private readonly ImageService _imageService;

    public CreateModel(BlogService blogService, ImageService imageService)
    {
        _blogService = blogService;
        _imageService = imageService;
    }

    [BindProperty]
    public BlogPost Post { get; set; } = new();

    [BindProperty]
    public string TagsString { get; set; } = "";

    [BindProperty]
    public IFormFile? FeaturedImageFile { get; set; }

    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _blogService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _blogService.GetAllCategoriesAsync();

        if (string.IsNullOrWhiteSpace(Post.Title))
        {
            ErrorMessage = "Title is required.";
            return Page();
        }

        // Handle image upload
        if (FeaturedImageFile != null)
        {
            var uploadResult = await _imageService.UploadImageAsync(FeaturedImageFile);
            if (uploadResult.Success)
            {
                Post.FeaturedImage = uploadResult.Url;
            }
            else
            {
                ErrorMessage = uploadResult.Error;
                return Page();
            }
        }

        // Parse tags
        if (!string.IsNullOrWhiteSpace(TagsString))
        {
            Post.Tags = TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
        }

        try
        {
            await _blogService.CreatePostAsync(Post);
            TempData["Message"] = "Post created successfully!";
            return RedirectToPage("/Admin/Posts/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating post: {ex.Message}";
            return Page();
        }
    }
}
