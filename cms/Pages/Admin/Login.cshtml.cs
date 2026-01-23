using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace InteronBlog.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly SiteSettings _settings;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IOptions<SiteSettings> settings, ILogger<LoginModel> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // If already authenticated, redirect to admin
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Index");
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Password is required";
            return Page();
        }

        // Verify password
        if (Password == _settings.AdminPassword)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Admin"),
                new Claim(ClaimTypes.Role, "Administrator")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Admin logged in successfully");

            return LocalRedirect(returnUrl ?? "/admin/");
        }

        _logger.LogWarning("Failed login attempt");
        ErrorMessage = "Invalid password";
        return Page();
    }
}
