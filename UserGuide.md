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

## Troubleshooting Network Issues (Expo Go)

If you see `Network request failed` on your mobile device, it is likely the **Windows Firewall** blocking the connection.

### How to fix Firewall
1.  Open **Windows Defender Firewall with Advanced Security**.
2.  Click **Inbound Rules** -> **New Rule...**.
3.  Select **Port** -> **Next**.
4.  Select **TCP** and enter **5055** in **Specific local ports** -> **Next**.
5.  Select **Allow the connection** -> **Next**.
6.  Check all profiles (Domain, Private, Public) -> **Next**.
7.  Name it "Conquest API" -> **Finish**.

Alternatively, run this command in an **Administrator** PowerShell:
```powershell
New-NetFirewallRule -DisplayName "Conquest API" -Direction Inbound -LocalPort 5055 -Protocol TCP -Action Allow
```