# ============================================
# AUTOMATED DEPLOYMENT SCRIPT
# Called by GitHub Actions - No user input required
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Automated Server Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory (repo root)
$repoPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "[1/7] Repository path: $repoPath" -ForegroundColor Yellow

# Step 2: Stop IIS app pool BEFORE building
Write-Host ""
Write-Host "[2/7] Stopping IIS app pool..." -ForegroundColor Yellow
try {
    # Use appcmd.exe instead of PowerShell module (more reliable)
    $appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    if (Test-Path $appcmd) {
        & $appcmd stop apppool /apppool.name:"InsightsPool" | Out-Null
        Start-Sleep -Seconds 3
        Write-Host "  [OK] App pool stopped" -ForegroundColor Green
    } else {
        throw "appcmd.exe not found"
    }
}
catch {
    Write-Host "  [WARNING] Could not stop app pool: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  Attempting to continue anyway..." -ForegroundColor Gray
}

# Step 3: Build and publish
Write-Host ""
Write-Host "[3/7] Building and publishing application..." -ForegroundColor Yellow
Set-Location "$repoPath\cms"
dotnet clean
dotnet restore
dotnet publish -c Release -o "C:\inetpub\wwwroot\InteronWeb\publish" --no-self-contained

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Build successful" -ForegroundColor Green
} else {
    Write-Host "  [FAILED] Build failed!" -ForegroundColor Red
    # Try to restart app pool even if build failed
    try { Start-WebAppPool -Name 'InsightsPool' -ErrorAction SilentlyContinue } catch {}
    exit 1
}

# Step 4: Verify CSS file
Write-Host ""
Write-Host "[4/7] Verifying deployment..." -ForegroundColor Yellow
$cssPath = "C:\inetpub\wwwroot\InteronWeb\publish\wwwroot\assets\css\styles.css"
if (Test-Path $cssPath) {
    $cssContent = Get-Content $cssPath -Raw
    if ($cssContent -match 'Main content area') {
        Write-Host "  [OK] CSS file with latest changes deployed" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] CSS file exists but may not have latest changes" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [WARNING] CSS file missing!" -ForegroundColor Red
}

# Step 5: Configure IIS (optional - may fail if WebAdministration not available)
Write-Host ""
Write-Host "[5/7] Configuring IIS..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop

    # Remove old sub-application
    $oldApp = Get-WebApplication -Site "Default Web Site" -Name "cms" -ErrorAction SilentlyContinue
    if ($oldApp) {
        Remove-WebApplication -Site "Default Web Site" -Name "cms"
        Write-Host "  Removed /cms sub-application" -ForegroundColor Gray
    }

    # Update site path
    Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\InteronWeb\publish"
    Set-ItemProperty "IIS:\Sites\Default Web Site" -Name applicationPool -Value "InsightsPool"

    Write-Host "  [OK] IIS configured" -ForegroundColor Green
}
catch {
    Write-Host "  [SKIPPED] IIS configuration (WebAdministration not available)" -ForegroundColor Yellow
    Write-Host "  App pool will auto-reload on file changes" -ForegroundColor Gray
}

# Step 6: Start app pool
Write-Host ""
Write-Host "[6/7] Starting application..." -ForegroundColor Yellow
try {
    # Use appcmd.exe instead of PowerShell module (more reliable)
    $appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    if (Test-Path $appcmd) {
        & $appcmd start apppool /apppool.name:"InsightsPool" | Out-Null
        Start-Sleep -Seconds 5
        Write-Host "  [OK] App pool started" -ForegroundColor Green
    } else {
        throw "appcmd.exe not found"
    }
}
catch {
    Write-Host "  [SKIPPED] App pool will auto-start" -ForegroundColor Yellow
}

# Step 7: Test deployment
Write-Host ""
Write-Host "[7/7] Testing deployment..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://interon.co.za/" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  [OK] Site responding with HTTP 200" -ForegroundColor Green
    }

    # Test CSS
    $cssResponse = Invoke-WebRequest -Uri "https://interon.co.za/assets/css/styles.css" -UseBasicParsing -TimeoutSec 10
    if ($cssResponse.StatusCode -eq 200 -and $cssResponse.Content -match 'Main content area') {
        Write-Host "  [OK] CSS file accessible with latest changes" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] CSS may need browser cache clear" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  [FAILED] Site test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

# Exit successfully (no pause for automated deployment)
exit 0
