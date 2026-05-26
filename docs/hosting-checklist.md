# EduBridge Hosting Checklist

## Required environment variables

Set these values on the hosting server instead of storing production secrets in `appsettings.json`.

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=YOUR_SQL_HOST;Database=EduBridgeDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;
DataProtection__KeysPath=D:\EduBridgeKeys
```

Change `AllowedHosts` in `appsettings.Production.json` to the real domain before publishing.

## IIS notes

- Install .NET 8 Hosting Bundle.
- App Pool: No Managed Code.
- Enable WebSocket Protocol if SignalR chat is used.
- Grant write permission to:
  - `wwwroot\uploads\teachers`
  - the configured `DataProtection__KeysPath`

## Database-first deploy

Run SQL schema scripts on the production database before deploying the application.
Do not run EF migrations automatically for this project.
