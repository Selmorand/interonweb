# Deploy script - Run as Administrator
Import-Module WebAdministration

Write-Host "Stopping app pool..." -ForegroundColor Yellow
Stop-WebAppPool -Name 'InsightsPool' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

Write-Host "Removing old publish folder..." -ForegroundColor Yellow
if (Test-Path "C:\inetpub\wwwroot\InteronWeb\publish") {
    Remove-Item "C:\inetpub\wwwroot\InteronWeb\publish" -Recurse -Force
}

Write-Host "Creating fresh directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\InteronWeb\publish" -Force | Out-Null

Write-Host "Copying ALL files..." -ForegroundColor Yellow
$sourceFiles = Get-ChildItem ".\publish-temp" -Recurse
$totalFiles = $sourceFiles.Count
Write-Host "  Copying $totalFiles files..."
Copy-Item -Path ".\publish-temp\*" -Destination "C:\inetpub\wwwroot\InteronWeb\publish\" -Recurse -Force

Write-Host "Verifying CSS file..." -ForegroundColor Yellow
if (Test-Path "C:\inetpub\wwwroot\InteronWeb\publish\wwwroot\assets\css\styles.css") {
    Write-Host "  ✓ CSS file deployed successfully" -ForegroundColor Green
} else {
    Write-Host "  ✗ WARNING: CSS file missing!" -ForegroundColor Red
}

Write-Host "Configuring IIS..." -ForegroundColor Yellow
$oldApp = Get-WebApplication -Site "Default Web Site" -Name "cms" -ErrorAction SilentlyContinue
if ($oldApp) {
    Remove-WebApplication -Site "Default Web Site" -Name "cms"
    Write-Host "  Removed old /cms sub-application"
}

Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\InteronWeb\publish"
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name applicationPool -Value "InsightsPool"

Write-Host "Starting app pool..." -ForegroundColor Yellow
Start-WebAppPool -Name 'InsightsPool'
Start-Sleep -Seconds 5

Write-Host "Testing deployment..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://interon.co.za/" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
        Write-Host "Site is live at: https://interon.co.za" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "✗ Site test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to close..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
