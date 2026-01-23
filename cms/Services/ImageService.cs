namespace InteronBlog.Services;

public class ImageService
{
    private readonly string _uploadsPath;
    private readonly ILogger<ImageService> _logger;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
    {
        _logger = logger;
        _uploadsPath = Path.Combine(env.WebRootPath, "uploads", "images");
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return new ImageUploadResult { Success = false, Error = "No file provided" };
        }

        if (file.Length > MaxFileSize)
        {
            return new ImageUploadResult { Success = false, Error = "File size exceeds 5MB limit" };
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new ImageUploadResult { Success = false, Error = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" };
        }

        try
        {
            // Generate unique filename
            var fileName = $"{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = $"/uploads/images/{fileName}";
            _logger.LogInformation("Uploaded image: {FileName} to {Path}", file.FileName, relativePath);

            return new ImageUploadResult
            {
                Success = true,
                Url = relativePath,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
            return new ImageUploadResult { Success = false, Error = "Error uploading file" };
        }
    }

    public bool DeleteImage(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        try
        {
            // Extract filename from URL
            var fileName = Path.GetFileName(url);
            var filePath = Path.Combine(_uploadsPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted image: {Path}", filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {Url}", url);
        }

        return false;
    }

    public List<string> GetAllImages()
    {
        try
        {
            if (!Directory.Exists(_uploadsPath))
                return new List<string>();

            return Directory.GetFiles(_uploadsPath)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => $"/uploads/images/{Path.GetFileName(f)}")
                .OrderByDescending(f => f)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing images");
            return new List<string>();
        }
    }
}

public class ImageUploadResult
{
    public bool Success { get; set; }
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Error { get; set; } = "";
}
