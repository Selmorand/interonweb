# Deployment Instructions for Clean Migration

## What Changed

We've migrated from a mixed static/dynamic architecture to a unified ASP.NET Core Razor Pages application:

**Before:**
- Static HTML files at root: `/about.html`, `/audit.html`, etc.
- CMS as sub-application: `/cms/insights/`, `/cms/admin/`
- Mixed URLs with and without `.html` extensions

**After:**
- Single unified ASP.NET Core app
- All pages as Razor Pages with clean URLs: `/about/`, `/audit/`, `/insights/`, etc.
- Consistent routing, no `.html` extensions
- Deployed to: `C:\inetpub\wwwroot\InteronWeb\publish\`

## Local Testing Completed ✓

The application has been tested locally and all pages are working:
- Home page: http://localhost:5000/
- About: http://localhost:5000/about/
- Audit: http://localhost:5000/audit/
- Contact: http://localhost:5000/contact/
- Services: http://localhost:5000/services/
- Learn: http://localhost:5000/learn/
- Insights: http://localhost:5000/insights/
- Admin: http://localhost:5000/admin/login

## Server Deployment Steps

### Step 1: Backup Current Setup

```powershell
# Stop the current site
Stop-WebAppPool -Name 'InsightsPool'

# Backup the old folder
Copy-Item -Path "C:\inetpub\wwwroot\Website" -Destination "C:\inetpub\wwwroot\Website_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')" -Recurse
```

### Step 2: Prepare Clean Deployment Location

```powershell
# Create new directory
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\InteronWeb" -Force
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\InteronWeb\publish" -Force
```

### Step 3: Build and Publish Locally

```powershell
cd "C:\Users\George\OneDrive - Interon\I\Interon\2026\Website\cms"

# Clean and publish
dotnet clean
dotnet publish -c Release -o "C:\inetpub\wwwroot\InteronWeb\publish" --no-self-contained
```

### Step 4: Configure IIS

```powershell
Import-Module WebAdministration

# Remove old sub-application if it exists
$oldApp = Get-WebApplication -Site "Default Web Site" -Name "cms" -ErrorAction SilentlyContinue
if ($oldApp) {
    Remove-WebApplication -Site "Default Web Site" -Name "cms"
    Write-Host "Removed old /cms sub-application"
}

# Update Default Web Site to point to new location
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\InteronWeb\publish"

# Verify app pool settings (use existing or create new)
$appPool = Get-WebAppPool -Name 'InteronWebPool' -ErrorAction SilentlyContinue
if (-not $appPool) {
    New-WebAppPool -Name 'InteronWebPool'
    Set-ItemProperty "IIS:\AppPools\InteronWebPool" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\InteronWebPool" -Name processModel.identityType -Value "ApplicationPoolIdentity"
}

# Assign app pool to site
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name applicationPool -Value 'InteronWebPool'

# Start the app pool
Start-WebAppPool -Name 'InteronWebPool'
```

### Step 5: Test the Deployment

```powershell
# Wait for app to start
Start-Sleep -Seconds 5

# Test key pages
$urls = @(
    "https://interon.co.za/",
    "https://interon.co.za/about/",
    "https://interon.co.za/audit/",
    "https://interon.co.za/insights/",
    "https://interon.co.za/admin/login"
)

foreach ($url in $urls) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop
        Write-Host "✓ $url - HTTP $($response.StatusCode)"
    } catch {
        Write-Host "✗ $url - FAILED: $_" -ForegroundColor Red
    }
}
```

### Step 6: Setup URL Redirects (Optional but Recommended)

To handle old `.html` URLs, add URL rewrite rules in `web.config`:

```xml
<system.webServer>
  <rewrite>
    <rules>
      <rule name="Remove .html Extension" stopProcessing="true">
        <match url="^(.*)\.html$" />
        <action type="Redirect" url="{R:1}/" redirectType="Permanent" />
      </rule>
      <rule name="Redirect /cms/ to root" stopProcessing="true">
        <match url="^cms/(.*)$" />
        <action type="Redirect" url="/{R:1}" redirectType="Permanent" />
      </rule>
    </rules>
  </rewrite>
</system.webServer>
```

This is automatically included in the publish output.

## Rollback Plan (If Needed)

If something goes wrong:

```powershell
# Stop new site
Stop-WebAppPool -Name 'InteronWebPool'

# Restore old configuration
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\Website"

# Recreate /cms sub-application
New-WebApplication -Site "Default Web Site" -Name "cms" -PhysicalPath "C:\inetpub\wwwroot\Website\cms\publish" -ApplicationPool 'InsightsPool'

# Start old app pool
Start-WebAppPool -Name 'InsightsPool'
```

## Troubleshooting

### Issue: Pages return 404
**Solution:** Ensure the app pool is running and the physical path is correct:
```powershell
Get-Website -Name "Default Web Site" | Select-Object Name, PhysicalPath, ApplicationPool
Get-WebAppPool -Name 'InteronWebPool' | Select-Object Name, State
```

### Issue: Static assets (CSS/JS) not loading
**Solution:** Check that `/assets/` folder is present in publish directory:
```powershell
Get-ChildItem "C:\inetpub\wwwroot\InteronWeb\publish\wwwroot\assets" | Select-Object Name
```

### Issue: Admin login not working
**Solution:** Verify `appsettings.json` has correct `AdminPassword`:
```powershell
Get-Content "C:\inetpub\wwwroot\InteronWeb\publish\appsettings.json" | ConvertFrom-Json | Select-Object -ExpandProperty SiteSettings
```

## Post-Deployment Checklist

- [ ] Home page loads correctly
- [ ] All navigation links work (no .html extensions)
- [ ] Audit tool loads and runs
- [ ] Insights/blog pages work
- [ ] Admin login works
- [ ] Static assets (CSS, JS, images) load
- [ ] Old .html URLs redirect to clean URLs
- [ ] HTTPS is working
- [ ] SSL certificate is valid

## Future GitHub Actions Deployment

The `.github/workflows/deploy.yml` has been updated to deploy to the new location automatically. You'll need to:

1. Update the git clone URL in deploy.yml (line 27)
2. Ensure GitHub secrets are configured
3. Test the automated deployment

## Support

If you encounter issues:
1. Check IIS Event Viewer logs
2. Check application logs in `C:\inetpub\wwwroot\InteronWeb\publish\logs\`
3. Verify file permissions on publish directory
