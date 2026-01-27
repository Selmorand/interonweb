# Deployment Guide

## Quick Deploy from VS Code

### Method 1: Quick Deploy Script (Recommended)
```powershell
.\quick-deploy.ps1
```

This script will:
1. Check for uncommitted changes and optionally commit them
2. Push to GitHub
3. Trigger automated deployment via GitHub Actions
4. Wait and verify the deployment succeeded

### Method 2: Manual Git Push
```powershell
git add .
git commit -m "Your commit message"
git push origin main
```

GitHub Actions will automatically deploy when you push to main branch.

### Method 3: VS Code Tasks
Press `Ctrl+Shift+P` and type "Run Task", then select:
- **"Deploy to Server (Push to GitHub)"** - Push existing commits
- **"Commit and Deploy"** - Commit and push in one step

---

## Manual Deployment on Server

If automated deployment isn't working, RDP to the server and run:

```powershell
cd C:\repos\Website
git pull origin main
.\SERVER-DEPLOY.ps1
```

---

## How Automated Deployment Works

1. **You push to GitHub** from VS Code
2. **GitHub Actions** triggers automatically
3. **SSH connects** to the server
4. **auto-deploy.ps1** runs on the server:
   - Pulls latest code from git
   - Builds the application
   - Deploys to `C:\inetpub\wwwroot\InteronWeb\publish`
   - Restarts the app pool
   - Tests the deployment

---

## Troubleshooting

### GitHub Actions isn't deploying

Check the workflow status:
- Go to https://github.com/Selmorand/interonweb/actions
- Click on the latest run to see logs
- If it completes in <20 seconds, SSH isn't working properly

**Fix:** RDP to server and deploy manually using `SERVER-DEPLOY.ps1`

### Changes aren't showing on the site

1. **Hard refresh your browser:** Ctrl+Shift+R
2. **Clear browser cache**
3. **Check if CSS is updated:**
   ```bash
   curl https://interon.co.za/assets/css/styles.css | grep "Main content area"
   ```

### Build fails

Check the error message, common issues:
- NuGet package restore failure → Run `dotnet restore` locally
- Compilation errors → Fix and test locally with `dotnet build`
- File locks → App pool is locking files, stop it first

---

## Development Workflow

1. **Make changes** in VS Code
2. **Test locally:**
   ```powershell
   cd cms
   dotnet run
   ```
   Open http://localhost:5000

3. **Commit changes:**
   ```powershell
   git add .
   git commit -m "Description of changes"
   ```

4. **Deploy:**
   ```powershell
   .\quick-deploy.ps1
   ```

5. **Verify:**
   - Visit https://interon.co.za
   - Test your changes
   - Check different pages

---

## File Locations

### Local Development
- **Source code:** `C:\Users\George\OneDrive - Interon\I\Interon\2026\Website`
- **CMS project:** `cms/`
- **Static assets:** `cms/wwwroot/`

### Server
- **Git repository:** `C:\repos\Website`
- **Deployed site:** `C:\inetpub\wwwroot\InteronWeb\publish`
- **IIS Site:** Default Web Site → Points to publish folder
- **App Pool:** InsightsPool

---

## Common Commands

### Local Development
```powershell
# Build the project
dotnet build cms/InteronBlog.csproj

# Run locally
cd cms && dotnet run

# Clean build artifacts
dotnet clean cms/InteronBlog.csproj

# Restore packages
dotnet restore cms/InteronBlog.csproj
```

### Git Commands
```powershell
# Check status
git status

# View recent commits
git log --oneline -5

# Undo last commit (keep changes)
git reset --soft HEAD~1

# Discard local changes
git restore .
```

### Deployment Scripts
- `quick-deploy.ps1` - Deploy from VS Code
- `SERVER-DEPLOY.ps1` - Run on server via RDP
- `auto-deploy.ps1` - Called by GitHub Actions
- `deploy.ps1` - Old FTP script (deprecated)
