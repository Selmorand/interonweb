# Interon Insights CMS

ASP.NET Core 8.0 Razor Pages application for managing blog content.

## ⚠️ CRITICAL WARNINGS

### web.config Configuration
**READ THIS FIRST:** See [CRITICAL-WEB-CONFIG-WARNING.md](CRITICAL-WEB-CONFIG-WARNING.md)

The web.config in this directory has a specific configuration required for IIS sub-application deployment. DO NOT modify it without reading the warning file first.

## Deployment

This application is deployed as an IIS sub-application at `/cms/` on the main Interon website.

### Automatic Deployment

GitHub Actions automatically deploys on push to `main`:
1. `git reset --hard origin/main` - Reset to latest commit
2. `git clean -fd` - Remove untracked files
3. `dotnet restore` - Restore NuGet packages
4. `dotnet publish -c Release -o publish` - Build and publish
5. Restart IIS application pool

### Manual Deployment

If you need to deploy manually:
```bash
cd C:\inetpub\wwwroot\Website
git fetch origin main
git reset --hard origin/main
git clean -fd
cd cms
dotnet restore
dotnet publish -c Release -o publish
powershell -Command "Restart-WebAppPool -Name 'InsightsPool'"
```

## Development

### Running Locally
```bash
cd cms
dotnet restore
dotnet run
```

Application will run at: http://localhost:5000

### Project Structure
- `Pages/` - Razor Pages (UI)
- `Services/` - Business logic (BlogService, ImageService)
- `Models/` - Data models
- `Data/` - JSON data storage (posts.json, categories.json)
- `wwwroot/` - Static files (CSS, JS, images)

### Data Storage

Blog posts and categories are stored in JSON files:
- `Data/posts.json` - All blog posts
- `Data/categories.json` - Blog categories

**IMPORTANT:** The deployment workflow preserves `cms/Data/` to avoid losing blog posts during deployment.

## Admin Access

- URL: https://interon.co.za/cms/Admin/Login
- Password: See `appsettings.json` → `SiteSettings:AdminPassword`

## Configuration

See `appsettings.json` for:
- Site settings (name, URL, description)
- Admin password
- API key (if API features are added)

## IIS Configuration

- Application Path: `/cms`
- Physical Path: `C:\inetpub\wwwroot\Website\cms\publish`
- Application Pool: `InsightsPool`
- .NET CLR Version: No Managed Code (uses ASP.NET Core Module)

## Troubleshooting

### 500 Errors
- Check web.config doesn't have `<location>` wrapper
- Check IIS app pool is running
- Test locally with `dotnet run`

### 404 Errors
- Verify `cms/publish` folder exists
- Verify publish folder contains `InteronBlog.dll`
- Check IIS application path is `/cms`

### Changes Not Deploying
- Check GitHub Actions workflow completed successfully
- Verify git commit was pushed to main branch
- Manually run deployment commands if needed
