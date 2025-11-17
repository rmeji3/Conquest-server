# User guide

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
dotnet ef migrations add AuthDBContextChanges --context Conquest.Data.App.AppDbContext
dotnet ef database update --context Conquest.Data.App.AppDbContext
```
### Run the following commands to migrate sqlite server (AppDBContext in this case):
```bash
dotnet ef migrations add AppDBContextChanges --context Conquest.Data.Auth.AppDBContext
dotnet ef database update --context Conquest.Data.Auth.AppDBContext
```
### Note:
These are the commands without names so you can replace the names with your own if needed.
```bash
dotnet ef migrations add <MigrationName> --context <YourDbContext>
dotnet ef database update --context <YourDbContext>
```