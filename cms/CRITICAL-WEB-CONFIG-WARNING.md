# ⚠️ CRITICAL: DO NOT MODIFY web.config ⚠️

## THE PROBLEM

The `web.config` file in this directory has a **SPECIFIC CONFIGURATION** that prevents the application from crashing when deployed as an IIS sub-application.

## WHAT NOT TO DO

**NEVER add this wrapper:**
```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <!-- config -->
    </system.webServer>
  </location>
</configuration>
```

**WHY?** Because this CMS runs as an IIS sub-application at `/cms/`, and the `<location>` wrapper causes IIS configuration conflicts that crash the app with 500 errors.

## CORRECT CONFIGURATION

The web.config should look like this:
```xml
<configuration>
  <system.webServer>
    <!-- config here -->
  </system.webServer>
</configuration>
```

Notice: **NO `<location>` wrapper**

## WHAT HAPPENS IF YOU BREAK IT

- ❌ All CMS pages return 500 Internal Server Error
- ❌ Even `/cms/api/ai/health` fails
- ❌ App starts then immediately shuts down on every request
- ❌ No error logs because app crashes before logging initializes

## TESTING LOCALLY

Before deploying web.config changes, ALWAYS test locally:
```bash
cd cms
dotnet run
```

Then test in browser at http://localhost:5000/Insights

## HISTORY

- **Jan 26, 2026**: Spent hours debugging 500 errors caused by `<location>` wrapper
- **Jan 27, 2026**: Rolled back to commit 9fe6a4e, encountered same issue again
- Commits with fix: 9728aa0, 5ae3bdc, 842b90b

## IF YOU MUST CHANGE web.config

1. Test locally first with `dotnet run`
2. Keep a backup of the working version
3. NEVER add the `<location>` wrapper
4. Commit to git before deploying
5. Be ready to rollback immediately if deployment fails
