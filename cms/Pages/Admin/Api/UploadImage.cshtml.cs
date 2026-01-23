using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin.Api;

[Authorize]
public class UploadImageModel : PageModel
{
    private readonly ImageService _imageService;

    public UploadImageModel(ImageService imageService)
    {
        _imageService = imageService;
    }

    public IActionResult OnGet()
    {
        return new JsonResult(new { error = "Method not allowed" });
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        if (file == null)
        {
            return new JsonResult(new { error = "No file provided" });
        }

        var result = await _imageService.UploadImageAsync(file);

        if (result.Success)
        {
            // Return in TinyMCE expected format
            return new JsonResult(new { location = result.Url });
        }

        return new JsonResult(new { error = result.Error });
    }
}
