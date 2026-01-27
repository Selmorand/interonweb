# IIS Configuration for Interon Blog CMS

## Critical Configuration

This document contains the **exact IIS configuration** needed for the CMS to work. If the site stops working, verify these settings.

## IIS Application Settings

### Application Configuration
- **Site**: Default Web Site
- **Application Path**: `/cms`
- **Physical Path**: `C:\inetpub\wwwroot\Website\cms\publish`
- **Application Pool**: InsightsPool

### Application Pool Settings
- **Name**: InsightsPool
- **.NET CLR Version**: No Managed Code (empty string)
- **Managed Pipeline Mode**: Integrated
- **Start Mode**: OnDemand
- **Enable 32-Bit Applications**: False

## PowerShell Commands to Recreate Configuration

If the IIS application needs to be recreated from scratch:

```powershell
# 1. Create the app pool (if it doesn't exist)
New-WebAppPool -Name "InsightsPool"
Set-ItemProperty "IIS:\AppPools\InsightsPool" -Name managedRuntimeVersion -Value ""

# 2. Remove any existing cms application
Remove-WebApplication -Name "cms" -Site "Default Web Site" -ErrorAction SilentlyContinue

# 3. Create the application
cd C:\Windows\System32\inetsrv
.\appcmd.exe add app /site.name:"Default Web Site" /path:/cms /physicalPath:"C:\inetpub\wwwroot\Website\cms\publish"
.\appcmd.exe set app "Default Web Site/cms" /applicationPool:InsightsPool

# 4. Unlock the aspNetCore section (required for web.config to work)
.\appcmd.exe unlock config -section:system.webServer/aspNetCore

# 5. Restart the app pool
Restart-WebAppPool -Name "InsightsPool"
```

## Verification Commands

Run these to verify the configuration is correct:

```powershell
# Check application exists with correct settings
Get-WebApplication -Site "Default Web Site" | Where-Object {$_.Path -like "*cms*"} | Format-Table Name, Path, PhysicalPath, ApplicationPool

# Expected output:
# Name  Path  PhysicalPath                           ApplicationPool
# ----  ----  ------------                           ---------------
#       /cms  C:\inetpub\wwwroot\Website\cms\publish InsightsPool

# Check app pool settings
Get-ItemProperty "IIS:\AppPools\InsightsPool" | Select-Object Name, State, ManagedRuntimeVersion

# Expected output:
# Name         State   ManagedRuntimeVersion
# ----         -----   ---------------------
# InsightsPool Started (empty string)

# Test the site
curl https://interon.co.za/cms/Insights -UseBasicParsing
# Expected: StatusCode 200
```

## Important Notes

1. **DO NOT** use IIS Manager GUI to create the application - it sometimes creates nested paths like `/cms/cmc`
2. **ALWAYS** use the appcmd commands above to ensure correct configuration
3. **The web.config file is auto-generated** by `dotnet publish` and should not be manually edited
4. **The application pool MUST have "No Managed Code"** - this is for .NET 8, not .NET Framework

## Troubleshooting

### Site returns 404
- Verify the IIS application path is exactly `/cms` (not `/cms/cmc`)
- Check the physical path points to the `publish` folder, not the source `cms` folder

### Site returns "connection closed" error
- This is normal for HTTP requests because the app redirects to HTTPS
- Always test with `https://` not `http://`

### Application won't start
- Check Event Viewer: `Get-EventLog -LogName Application -Source "IIS*" -Newest 5`
- Check stdout logs: `Get-ChildItem "C:\inetpub\wwwroot\Website\cms\publish\logs"`
- Verify the DLL exists: `Test-Path "C:\inetpub\wwwroot\Website\cms\publish\InteronBlog.dll"`

## Deployment Process

The GitHub Actions workflow automatically:
1. Pulls latest code
2. Cleans old publish folder
3. Runs `dotnet publish`
4. Restarts the InsightsPool app pool

The IIS application configuration is **NOT changed** during deployment - it persists across deploys.
