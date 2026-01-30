using InteronBlog.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace InteronBlog.Services;

public class UserAuthenticationService
{
    private readonly string _usersFilePath;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<UserAuthenticationService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    // Account lockout settings
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public UserAuthenticationService(
        IWebHostEnvironment env,
        IPasswordHasher<User> passwordHasher,
        ILogger<UserAuthenticationService> logger)
    {
        _usersFilePath = Path.Combine(env.ContentRootPath, "App_Data", "users.json");
        _passwordHasher = passwordHasher;
        _logger = logger;

        // Ensure App_Data directory exists
        var directory = Path.GetDirectoryName(_usersFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize with default admin user if no users exist
        InitializeDefaultUser().Wait();
    }

    private async Task InitializeDefaultUser()
    {
        var users = await GetAllUsersAsync();
        if (users.Count == 0)
        {
            _logger.LogInformation("No users found. Creating default admin user.");

            var defaultUser = new User
            {
                Username = "admin",
                Role = "Administrator",
                IsActive = true
            };

            // Hash the default password from appsettings
            defaultUser.PasswordHash = _passwordHasher.HashPassword(defaultUser, "qz2rg4QZ@RG$");

            await CreateUserAsync(defaultUser);
            _logger.LogInformation("Default admin user created successfully.");
        }
    }

    private async Task<List<User>> GetAllUsersAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_usersFilePath))
            {
                return new List<User>();
            }

            var json = await File.ReadAllTextAsync(_usersFilePath);
            var users = JsonSerializer.Deserialize<List<User>>(json);
            return users ?? new List<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading users file");
            return new List<User>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveUsersAsync(List<User> users)
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_usersFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving users file");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var users = await GetAllUsersAsync();
        return users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
            return null;
        }

        // Check if account is locked
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account: {Username}. Locked until: {LockedUntil}",
                username, user.LockedUntil.Value);
            return null;
        }

        // Unlock account if lockout period has passed
        if (user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.UtcNow)
        {
            user.LockedUntil = null;
            user.FailedLoginAttempts = 0;
        }

        // Verify password
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Success ||
            result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Successful login - reset failed attempts
            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            user.LockedUntil = null;

            await UpdateUserAsync(user);

            _logger.LogInformation("Successful login for user: {Username}", username);
            return user;
        }
        else
        {
            // Failed login - increment failed attempts
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("Account locked due to too many failed attempts: {Username}. Locked until: {LockedUntil}",
                    username, user.LockedUntil.Value);
            }
            else
            {
                _logger.LogWarning("Failed login attempt for user: {Username}. Attempts: {Attempts}/{Max}",
                    username, user.FailedLoginAttempts, MaxFailedAttempts);
            }

            await UpdateUserAsync(user);
            return null;
        }
    }

    public async Task CreateUserAsync(User user)
    {
        var users = await GetAllUsersAsync();

        // Check if username already exists
        if (users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"User with username '{user.Username}' already exists.");
        }

        users.Add(user);
        await SaveUsersAsync(users);

        _logger.LogInformation("User created: {Username}", user.Username);
    }

    public async Task UpdateUserAsync(User user)
    {
        var users = await GetAllUsersAsync();
        var index = users.FindIndex(u => u.Id == user.Id);

        if (index == -1)
        {
            throw new InvalidOperationException($"User with ID '{user.Id}' not found.");
        }

        users[index] = user;
        await SaveUsersAsync(users);
    }

    public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await ValidateUserAsync(username, currentPassword);
        if (user == null)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await UpdateUserAsync(user);

        _logger.LogInformation("Password changed for user: {Username}", username);
        return true;
    }

    public async Task<List<User>> GetAllAdminUsersAsync()
    {
        var users = await GetAllUsersAsync();
        return users.Where(u => u.Role == "Administrator").ToList();
    }

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }
}
