# User guide

## Redis Setup (Mac)
You can install Redis using Homebrew or Docker.

### Option 1: Homebrew (Recommended)
```bash
brew install redis
brew services start redis
```

### Option 2: Docker
```bash
docker run --name redis -d -p 6379:6379 redis
```

## Migrating sqlite Servers
If you get the following error:
```
Unhandled exception. System.InvalidOperationException: An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': 
The model for context 'AuthDbContext' has pending changes. Add a new migration before updating the database. See https://aka.ms/efcore-docs-pending-changes. 
This exception can be suppressed or logged by passing event ID 'RelationalEventId.PendingModelChangesWarning' to the 'ConfigureWarnings' method in 
'DbContext.OnConfiguring' or 'AddDbContext'.
```
### Run the following commands to migrate sqlite server (AuthDBContext in this case):
```bash
dotnet ef migrations add initAuthDb --context Conquest.Data.Auth.AuthDbContext
dotnet ef database update --context Conquest.Data.Auth.AuthDbContext
```
### Run the following commands to migrate sqlite server (AppDBContext in this case):
```bash
dotnet ef migrations add initAppDb --context Conquest.Data.App.AppDBContext
dotnet ef database update --context Conquest.Data.App.AppDBContext
```
### Note:
These are the commands without names so you can replace the names with your own if needed.
```bash
dotnet ef migrations add <MigrationName> --context <YourDbContext>
dotnet ef database update --context <YourDbContext>
```