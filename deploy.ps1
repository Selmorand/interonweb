# Interon Website Deployment Script
# Deploys to IIS server via FTP

param(
    [switch]$WhatIf
)

# Load credentials from .env.deploy (not committed to git)
$envFile = Join-Path $PSScriptRoot ".env.deploy"
if (-not (Test-Path $envFile)) {
    Write-Host "Creating .env.deploy file..." -ForegroundColor Yellow
    @"
FTP_HOST=41.185.20.42
FTP_USER=AzureAD\George
FTP_PASS=YOUR_PASSWORD_HERE
FTP_PATH=/Website
"@ | Out-File -FilePath $envFile -Encoding UTF8
    Write-Host "Please edit .env.deploy with your FTP password, then run this script again." -ForegroundColor Red
    exit 1
}

# Parse .env file
$config = @{}
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        $config[$matches[1]] = $matches[2]
    }
}

$ftpHost = $config['FTP_HOST']
$ftpUser = $config['FTP_USER']
$ftpPass = $config['FTP_PASS']
$ftpPath = $config['FTP_PATH']

# Convert Windows path to FTP path format
if ($ftpPath -match '^[A-Za-z]:') {
    # Windows path like C:\inetpub\wwwroot\Website -> /inetpub/wwwroot/Website
    $ftpPath = $ftpPath -replace '^[A-Za-z]:', ''
    $ftpPath = $ftpPath -replace '\\', '/'
}
# Ensure path starts with /
if ($ftpPath -and -not $ftpPath.StartsWith('/')) {
    $ftpPath = "/$ftpPath"
}
# Ensure path ends with /
if ($ftpPath -and -not $ftpPath.EndsWith('/')) {
    $ftpPath = "$ftpPath/"
}

if ($ftpPass -eq 'YOUR_PASSWORD_HERE') {
    Write-Host "Please edit .env.deploy with your FTP password." -ForegroundColor Red
    exit 1
}

Write-Host "=== Interon Website Deployment ===" -ForegroundColor Cyan
Write-Host "Target: ftp://$ftpHost$ftpPath" -ForegroundColor Gray

# Files and folders to deploy
$deployItems = @(
    "index.html",
    "audit.html",
    "about.html",
    "contact.html",
    "404.html",
    "web.config",
    "assets",
    "learn",
    "services"
)

# Create FTP session
$ftpUri = "ftp://$ftpHost$ftpPath"

function Upload-FtpFile {
    param($LocalPath, $RemotePath)

    $uri = "$ftpUri/$RemotePath"

    if ($WhatIf) {
        Write-Host "[WhatIf] Would upload: $LocalPath -> $uri" -ForegroundColor Yellow
        return
    }

    try {
        $webclient = New-Object System.Net.WebClient
        $webclient.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
        $webclient.UploadFile($uri, $LocalPath)
        Write-Host "  Uploaded: $RemotePath" -ForegroundColor Green
    }
    catch {
        Write-Host "  Failed: $RemotePath - $_" -ForegroundColor Red
    }
}

function Upload-FtpDirectory {
    param($LocalDir, $RemoteDir)

    # Create remote directory
    $uri = "$ftpUri/$RemoteDir"
    try {
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
        $response = $request.GetResponse()
        $response.Close()
    }
    catch {
        # Directory might already exist, ignore error
    }

    # Upload files in directory
    Get-ChildItem -Path $LocalDir -File | ForEach-Object {
        $remotePath = "$RemoteDir/$($_.Name)"
        Upload-FtpFile -LocalPath $_.FullName -RemotePath $remotePath
    }

    # Recurse into subdirectories
    Get-ChildItem -Path $LocalDir -Directory | ForEach-Object {
        $subRemoteDir = "$RemoteDir/$($_.Name)"
        Upload-FtpDirectory -LocalDir $_.FullName -RemoteDir $subRemoteDir
    }
}

# Deploy each item
$scriptDir = $PSScriptRoot
foreach ($item in $deployItems) {
    $localPath = Join-Path $scriptDir $item

    if (Test-Path $localPath -PathType Container) {
        Write-Host "Uploading directory: $item" -ForegroundColor Cyan
        Upload-FtpDirectory -LocalDir $localPath -RemoteDir $item
    }
    elseif (Test-Path $localPath -PathType Leaf) {
        Write-Host "Uploading file: $item" -ForegroundColor Cyan
        Upload-FtpFile -LocalPath $localPath -RemotePath $item
    }
    else {
        Write-Host "Not found: $item" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host "Visit: https://interon.co.za" -ForegroundColor Cyan
