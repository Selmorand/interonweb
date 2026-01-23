using InteronBlog.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "InteronBlog.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddSingleton<BlogService>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<SchemaService>();

// Configure SiteSettings
builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("SiteSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

public class SiteSettings
{
    public string SiteName { get; set; } = "Interon Blog";
    public string SiteUrl { get; set; } = "https://interon.co.za";
    public string SiteDescription { get; set; } = "";
    public string OrganizationName { get; set; } = "Interon";
    public string AdminPassword { get; set; } = "";
}
