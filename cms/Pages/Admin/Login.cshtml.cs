using InteronBlog.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace InteronBlog.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly UserAuthenticationService _authService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(UserAuthenticationService authService, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public string Username { get; set; } = "";

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
        if (string.IsNullOrEmpty(Username))
        {
            ErrorMessage = "Username is required";
            return Page();
        }

        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Password is required";
            return Page();
        }

        // Validate user credentials
        var user = await _authService.ValidateUserAsync(Username, Password);

        if (user != null)
        {
            // Successful login
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, user.Role)
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

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return LocalRedirect(returnUrl ?? "/admin/");
        }

        // Check if account is locked
        var lockedUser = await _authService.GetUserByUsernameAsync(Username);
        if (lockedUser?.LockedUntil.HasValue == true && lockedUser.LockedUntil.Value > DateTime.UtcNow)
        {
            var minutesRemaining = (int)(lockedUser.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            ErrorMessage = $"Account is locked due to too many failed login attempts. Please try again in {minutesRemaining} minutes.";
        }
        else
        {
            ErrorMessage = "Invalid username or password";
        }

        return Page();
    }
}
