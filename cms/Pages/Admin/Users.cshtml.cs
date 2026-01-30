using InteronBlog.Models;
using InteronBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class UsersModel : PageModel
{
    private readonly UserAuthenticationService _authService;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(UserAuthenticationService authService, ILogger<UsersModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public List<User> Users { get; set; } = new();

    [BindProperty]
    public string NewUsername { get; set; } = "";

    [BindProperty]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    public string CurrentPassword { get; set; } = "";

    [BindProperty]
    public string NewPasswordForChange { get; set; } = "";

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Users = await _authService.GetAllAdminUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Username and password are required";
            Users = await _authService.GetAllAdminUsersAsync();
            return Page();
        }

        if (NewPassword.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters long";
            Users = await _authService.GetAllAdminUsersAsync();
            return Page();
        }

        try
        {
            var newUser = new User
            {
                Username = NewUsername,
                Role = "Administrator",
                IsActive = true
            };

            newUser.PasswordHash = _authService.HashPassword(newUser, NewPassword);
            await _authService.CreateUserAsync(newUser);

            SuccessMessage = $"User '{NewUsername}' created successfully";
            _logger.LogInformation("New user created: {Username}", NewUsername);

            // Clear form
            NewUsername = "";
            NewPassword = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error creating user");
        }

        Users = await _authService.GetAllAdminUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var currentUsername = User.Identity?.Name;

        if (string.IsNullOrEmpty(currentUsername))
        {
            ErrorMessage = "User not found";
            Users = await _authService.GetAllAdminUsersAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPasswordForChange))
        {
            ErrorMessage = "Current password and new password are required";
            Users = await _authService.GetAllAdminUsersAsync();
            return Page();
        }

        if (NewPasswordForChange.Length < 8)
        {
            ErrorMessage = "New password must be at least 8 characters long";
            Users = await _authService.GetAllAdminUsersAsync();
            return Page();
        }

        var success = await _authService.ChangePasswordAsync(currentUsername, CurrentPassword, NewPasswordForChange);

        if (success)
        {
            SuccessMessage = "Password changed successfully";
            _logger.LogInformation("Password changed for user: {Username}", currentUsername);
            CurrentPassword = "";
            NewPasswordForChange = "";
        }
        else
        {
            ErrorMessage = "Current password is incorrect";
        }

        Users = await _authService.GetAllAdminUsersAsync();
        return Page();
    }
}
