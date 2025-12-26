# PowerShell script to drop the Ping databases
# Requires dotnet-ef tool installed

Write-Host "Dropping Databases..." -ForegroundColor Yellow

# Auth Database
Write-Host "Dropping Auth Database..." -ForegroundColor Cyan
dotnet ef database drop --context AuthDbContext --force

# App Database
Write-Host "Dropping App Database..." -ForegroundColor Cyan
dotnet ef database drop --context AppDbContext --force

Write-Host "Done. Run 'dotnet run' to re-create and migrate." -ForegroundColor Green
