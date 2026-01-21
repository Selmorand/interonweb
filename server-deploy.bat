@echo off
REM Run this script ON THE SERVER to deploy from GitHub
REM First time: git clone https://github.com/Selmorand/interonweb.git
REM Updates: git pull

set REPO_URL=https://github.com/Selmorand/interonweb.git
set DEPLOY_PATH=C:\inetpub\wwwroot\Website
set TEMP_PATH=C:\temp\interonweb

echo === Interon Website Server Deployment ===
echo.

REM Check if git is installed
where git >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Git is not installed. Please install Git for Windows.
    echo Download from: https://git-scm.com/download/win
    pause
    exit /b 1
)

REM Clone or pull the repo
if exist "%TEMP_PATH%\.git" (
    echo Updating repository...
    cd /d "%TEMP_PATH%"
    git pull
) else (
    echo Cloning repository...
    if exist "%TEMP_PATH%" rmdir /s /q "%TEMP_PATH%"
    git clone %REPO_URL% "%TEMP_PATH%"
)

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Git operation failed.
    pause
    exit /b 1
)

echo.
echo Copying files to %DEPLOY_PATH%...

REM Create deploy directory if it doesn't exist
if not exist "%DEPLOY_PATH%" mkdir "%DEPLOY_PATH%"

REM Copy website files (excluding git and dev files)
xcopy "%TEMP_PATH%\*.html" "%DEPLOY_PATH%\" /Y
xcopy "%TEMP_PATH%\web.config" "%DEPLOY_PATH%\" /Y
xcopy "%TEMP_PATH%\assets" "%DEPLOY_PATH%\assets\" /E /I /Y
xcopy "%TEMP_PATH%\learn" "%DEPLOY_PATH%\learn\" /E /I /Y
xcopy "%TEMP_PATH%\services" "%DEPLOY_PATH%\services\" /E /I /Y

echo.
echo === Deployment Complete ===
echo Website deployed to: %DEPLOY_PATH%
echo.
pause
