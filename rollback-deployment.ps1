# Rollback Script - Restore Previous Configuration
# Run this script if deployment fails and you need to rollback

Write-Host "========================================" -ForegroundColor Red
Write-Host "  ROLLBACK TO PREVIOUS VERSION" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

# Stop new app pool
Write-Host "Stopping InsightsPool..." -ForegroundColor Yellow
Stop-WebAppPool -Name 'InsightsPool' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Restore old configuration
Write-Host "Restoring previous IIS configuration..." -ForegroundColor Yellow
Import-Module WebAdministration

# Restore physical path to old location
Set-ItemProperty "IIS:\Sites\Default Web Site" -Name physicalPath -Value "C:\inetpub\wwwroot\Website"

# Recreate /cms sub-application
New-WebApplication -Site "Default Web Site" -Name "cms" -PhysicalPath "C:\inetpub\wwwroot\Website\cms\publish" -ApplicationPool 'InsightsPool' -ErrorAction SilentlyContinue

# Start app pool
Write-Host "Starting application..." -ForegroundColor Yellow
Start-WebAppPool -Name 'InsightsPool'
Start-Sleep -Seconds 5

# Test
Write-Host ""
Write-Host "Testing rollback..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://interon.co.za/cms/" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Site is responding" -ForegroundColor Green
        Write-Host ""
        Write-Host "Rollback complete. Old version restored." -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Site test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check IIS configuration manually" -ForegroundColor Yellow
}
