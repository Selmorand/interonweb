# ============================================
# RUN THIS SCRIPT ON THE SERVER VIA RDP
# Run PowerShell as Administrator on the SERVER
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Server Deployment Script" -ForegroundColor Cyan
Write-Host "  Run this ON THE SERVER via RDP" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if running on server
if (-not (Test-Path "C:\inetpub")) {
    Write-Host "ERROR: This must be run ON THE SERVER, not on your local machine!" -ForegroundColor Red
    Write-Host "Please Remote Desktop to the server first." -ForegroundColor Yellow
    pause
    exit 1
}

# Step 1: Find where the code repository is on the server
Write-Host "[1/7] Locating code repository on server..." -ForegroundColor Yellow

$possiblePaths = @(
    "C:\inetpub\wwwroot\Website",
    "C:\Users\Administrator\Website",
    "C:\Website",
    "C:\inetpub\wwwroot\interonweb"
)

$repoPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path "$path\.git") {
        $repoPath = $path
        break
    }
}

if (-not $repoPath) {
    Write-Host "ERROR: Cannot find git repository on server!" -ForegroundColor Red
    Write-Host "Please provide the path manually:" -ForegroundColor Yellow
    $repoPath = Read-Host "Enter full path to Website folder"

    if (-not (Test-Path "$repoPath\.git")) {
        Write-Host "ERROR: Not a valid git repository!" -ForegroundColor Red
        pause
        exit 1
    }
}

Write-Host "  Found repository at: $repoPath" -ForegroundColor Green

# Step 2: Pull latest code
Write-Host ""
Write-Host "[2/7] Pulling latest code from GitHub..." -ForegroundColor Yellow
Set-Location $repoPath
git fetch origin main
git reset --hard origin/main
Write-Host "  ✓ Code updated" -ForegroundColor Green

# Step 3: Build and publish
Write-Host ""
Write-Host "[3/7] Building and publishing application..." -ForegroundColor Yellow
Set-Location "$repoPath\cms"
dotnet clean
dotnet restore
dotnet publish -c Release -o "C:\inetpub\wwwroot\InteronWeb\publish" --no-self-contained

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Build successful" -ForegroundColor Green
} else {
    Write-Host "  ✗ Build failed!" -ForegroundColor Red
    pause
    exit 1
}

# Step 4: Verify CSS file
Write-Host ""
Write-Host "[4/7] Verifying deployment..." -ForegroundColor Yellow
if (Test-Path "C:\inetpub\wwwroot\InteronWeb\publish\wwwroot\assets\css\styles.css") {
    $cssContent = Get-Content "C:\inetpub\wwwroot\InteronWeb\publish\wwwroot\assets\css\styles.css" -Raw
    if ($cssContent -match "Main content area") {
        Write-Host "  ✓ CSS file with latest changes deployed" -ForegroundColor Green
    } else {
        Write-Host "  ! CSS file exists but may not have latest changes" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✗ WARNING: CSS file missing!" -ForegroundColor Red
}

# Step 5: Configure IIS
Write-Host ""
Write-Host "[5/7] Configuring IIS..." -ForegroundColor Yellow
Import-Module WebAdministration

# Check current configuration
Write-Host "  Current IIS configuration:" -ForegroundColor Gray
$site = Get-Website -Name "Default Web Site"
Write-Host "    Site path: $($site.PhysicalPath)" -ForegroundColor Gray

$oldApp = Get-WebApplication -Site "Default Web Site" -Name "cms" -ErrorAction SilentlyContinue
if ($oldApp) {
    Write-Host "    /cms sub-app: $($oldApp.PhysicalPath)" -ForegroundColor Gray
}

# Stop app pool
Write-Host "  Stopping app pool..." -ForegroundColor Gray
Stop-WebAppPool -Name 'InsightsPool' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# Remove old sub-application
if ($oldApp) {
    Remove-WebApplication -Site "Default Web Site" -Name "cms"
    Write-Host "  Removed /cms sub-application" -ForegroundColor Gray
}

# Update site path
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\InteronWeb\publish"
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name applicationPool -Value "InsightsPool"

Write-Host "  ✓ IIS configured" -ForegroundColor Green
Write-Host "    New site path: C:\inetpub\wwwroot\InteronWeb\publish" -ForegroundColor Gray

# Step 6: Start app pool
Write-Host ""
Write-Host "[6/7] Starting application..." -ForegroundColor Yellow
Start-WebAppPool -Name 'InsightsPool'
Start-Sleep -Seconds 5
Write-Host "  ✓ App pool started" -ForegroundColor Green

# Step 7: Test
Write-Host ""
Write-Host "[7/7] Testing deployment..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://interon.co.za/" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ Site responding with HTTP 200" -ForegroundColor Green
    }

    # Test CSS
    $cssResponse = Invoke-WebRequest -Uri "https://interon.co.za/assets/css/styles.css" -UseBasicParsing -TimeoutSec 10
    if ($cssResponse.StatusCode -eq 200 -and $cssResponse.Content -match "Main content area") {
        Write-Host "  ✓ CSS file accessible with latest changes" -ForegroundColor Green
    } else {
        Write-Host "  ! CSS may need browser cache clear" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ✗ Site test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Clear browser cache (Ctrl+Shift+R)" -ForegroundColor Gray
Write-Host "  2. Test: https://interon.co.za/about/" -ForegroundColor Gray
Write-Host "  3. Check spacing below header" -ForegroundColor Gray
Write-Host ""
pause
