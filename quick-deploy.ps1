# ============================================
# QUICK DEPLOYMENT FROM VS CODE
# Run this from VS Code terminal: .\quick-deploy.ps1
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Quick Deploy to Server" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if there are uncommitted changes
$status = git status --porcelain
if ($status) {
    Write-Host "[WARNING] You have uncommitted changes:" -ForegroundColor Yellow
    git status --short
    Write-Host ""
    $commit = Read-Host "Do you want to commit these changes? (y/n)"

    if ($commit -eq 'y' -or $commit -eq 'Y') {
        $message = Read-Host "Commit message"
        if ([string]::IsNullOrWhiteSpace($message)) {
            $message = "Update website"
        }
        git add .
        git commit -m $message
        Write-Host "  [OK] Changes committed" -ForegroundColor Green
    } else {
        Write-Host "  [SKIPPED] Proceeding without committing" -ForegroundColor Yellow
    }
}

# Push to GitHub
Write-Host ""
Write-Host "[1/2] Pushing to GitHub..." -ForegroundColor Yellow
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Pushed to GitHub" -ForegroundColor Green
} else {
    Write-Host "  [FAILED] Push failed!" -ForegroundColor Red
    pause
    exit 1
}

# GitHub Actions will auto-deploy
Write-Host ""
Write-Host "[2/2] Monitoring GitHub Actions deployment..." -ForegroundColor Yellow
Write-Host "  View live progress: https://github.com/Selmorand/interonweb/actions" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Waiting 30 seconds for deployment..." -ForegroundColor Gray
Start-Sleep -Seconds 30

# Test if deployment succeeded
Write-Host ""
Write-Host "Testing deployment..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://interon.co.za/" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  [OK] Site is live and responding!" -ForegroundColor Green
    }
}
catch {
    Write-Host "  [WARNING] Could not verify deployment" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Clear browser cache (Ctrl+Shift+R)" -ForegroundColor Gray
Write-Host "  2. Test your changes at https://interon.co.za" -ForegroundColor Gray
Write-Host ""
Write-Host "If changes aren't showing:" -ForegroundColor Yellow
Write-Host "  - RDP to server and run: cd C:\repos\Website; git pull; .\SERVER-DEPLOY.ps1" -ForegroundColor Gray
Write-Host ""
pause
