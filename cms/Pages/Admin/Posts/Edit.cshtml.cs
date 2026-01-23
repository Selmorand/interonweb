using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin.Posts;

[Authorize]
public class EditModel : PageModel
{
    private readonly BlogService _blogService;
    private readonly ImageService _imageService;

    public EditModel(BlogService blogService, ImageService imageService)
    {
        _blogService = blogService;
        _imageService = imageService;
    }

    [BindProperty]
    public BlogPost? Post { get; set; }

    [BindProperty]
    public string TagsString { get; set; } = "";

    [BindProperty]
    public IFormFile? FeaturedImageFile { get; set; }

    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return RedirectToPage("/Admin/Posts/Index");
        }

        Categories = await _blogService.GetAllCategoriesAsync();
        Post = await _blogService.GetPostByIdAsync(id);

        if (Post == null)
        {
            return Page();
        }

        TagsString = string.Join(", ", Post.Tags);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        Categories = await _blogService.GetAllCategoriesAsync();

        if (Post == null || string.IsNullOrWhiteSpace(Post.Title))
        {
            ErrorMessage = "Title is required.";
            return Page();
        }

        Post.Id = id;

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
        else
        {
            Post.Tags = new List<string>();
        }

        try
        {
            var updated = await _blogService.UpdatePostAsync(Post);
            if (updated == null)
            {
                ErrorMessage = "Post not found.";
                return Page();
            }

            TempData["Message"] = "Post updated successfully!";
            return RedirectToPage("/Admin/Posts/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating post: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var result = await _blogService.DeletePostAsync(id);
        TempData["Message"] = result ? "Post deleted successfully." : "Failed to delete post.";
        return RedirectToPage("/Admin/Posts/Index");
    }
}
