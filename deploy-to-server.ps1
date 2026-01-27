# Deployment Script - Migrate to Clean Architecture
# Run this script on the SERVER (not locally)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Interon Website - Clean Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Backup
Write-Host "[1/6] Creating backup..." -ForegroundColor Yellow
$backupName = "Website_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
$backupPath = "C:\inetpub\wwwroot\$backupName"

if (Test-Path "C:\inetpub\wwwroot\Website") {
    Write-Host "Backing up current site to: $backupPath"
    Copy-Item -Path "C:\inetpub\wwwroot\Website" -Destination $backupPath -Recurse -Force
    Write-Host "✓ Backup complete" -ForegroundColor Green
}
else {
    Write-Host "No existing Website folder to backup" -ForegroundColor Gray
}

# Step 2: Stop current app pool
Write-Host ""
Write-Host "[2/6] Stopping current app pool..." -ForegroundColor Yellow
try {
    Stop-WebAppPool -Name 'InsightsPool' -ErrorAction SilentlyContinue
    Write-Host "✓ App pool stopped" -ForegroundColor Green
}
catch {
    Write-Host "App pool not running or doesn't exist" -ForegroundColor Gray
}

Start-Sleep -Seconds 2

# Step 3: Create new directory structure
Write-Host ""
Write-Host "[3/6] Creating deployment directories..." -ForegroundColor Yellow
if (-not (Test-Path "C:\inetpub\wwwroot\InteronWeb")) {
    New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\InteronWeb" -Force | Out-Null
}
if (-not (Test-Path "C:\inetpub\wwwroot\InteronWeb\publish")) {
    New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\InteronWeb\publish" -Force | Out-Null
}
Write-Host "✓ Directories created" -ForegroundColor Green

# Step 4: Build and publish
Write-Host ""
Write-Host "[4/6] Building and publishing application..." -ForegroundColor Yellow
$sourcePath = "C:\Users\$env:USERNAME\OneDrive - Interon\I\Interon\2026\Website\cms"

if (Test-Path $sourcePath) {
    Set-Location $sourcePath

    Write-Host "  - Restoring packages..."
    dotnet restore

    Write-Host "  - Publishing to production..."
    dotnet publish -c Release -o "C:\inetpub\wwwroot\InteronWeb\publish" --no-self-contained

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build and publish complete" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Build failed!" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "✗ Source path not found: $sourcePath" -ForegroundColor Red
    Write-Host "Please update the script with the correct path" -ForegroundColor Red
    exit 1
}

# Step 5: Configure IIS
Write-Host ""
Write-Host "[5/6] Configuring IIS..." -ForegroundColor Yellow
Import-Module WebAdministration

# Remove old sub-application if it exists
$oldApp = Get-WebApplication -Site "Default Web Site" -Name "cms" -ErrorAction SilentlyContinue
if ($oldApp) {
    Remove-WebApplication -Site "Default Web Site" -Name "cms"
    Write-Host "  - Removed old /cms sub-application" -ForegroundColor Gray
}

# Update Default Web Site physical path
Write-Host "  - Updating site physical path..."
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\InteronWeb\publish"

# Use existing InsightsPool
Write-Host "  - Assigning app pool..."
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name applicationPool -Value 'InsightsPool'

Write-Host "✓ IIS configured" -ForegroundColor Green

# Step 6: Start app pool and test
Write-Host ""
Write-Host "[6/6] Starting application..." -ForegroundColor Yellow
Start-WebAppPool -Name 'InsightsPool'
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Testing deployment..." -ForegroundColor Yellow
$testUrls = @(
    "https://interon.co.za/",
    "https://interon.co.za/about/",
    "https://interon.co.za/audit/",
    "https://interon.co.za/insights/"
)

$allPassed = $true
foreach ($url in $testUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✓ $url" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ $url - HTTP $($response.StatusCode)" -ForegroundColor Red
            $allPassed = $false
        }
    }
    catch {
        Write-Host "  ✗ $url - FAILED: $($_.Exception.Message)" -ForegroundColor Red
        $allPassed = $false
    }
}

Write-Host ""
if ($allPassed) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  DEPLOYMENT SUCCESSFUL! 🚀" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your site is now live at: https://interon.co.za" -ForegroundColor Cyan
    Write-Host "Backup location: $backupPath" -ForegroundColor Gray
}
else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  WARNING: Some tests failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check the following:" -ForegroundColor Yellow
    Write-Host "  1. IIS logs: C:\inetpub\wwwroot\InteronWeb\publish\logs\" -ForegroundColor Gray
    Write-Host "  2. Event Viewer: Application logs" -ForegroundColor Gray
    Write-Host "  3. IIS Manager: Site status and app pool" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To rollback, run: .\rollback-deployment.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  - Test admin login: https://interon.co.za/admin/login" -ForegroundColor Gray
Write-Host "  - Verify all pages load correctly" -ForegroundColor Gray
Write-Host "  - Check that old .html URLs redirect properly" -ForegroundColor Gray
