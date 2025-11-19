# Conquest Server

Conquest is an ASP.NET Core API for managing users, places, activities, events, and friendships.

## 📚 Documentation

- **[Server Guide](ServerGuide.md)**: The authoritative source for architecture, endpoints, database schema, and business rules. **Consult this first.**
- **[Agent Instructions](.github/copilot-instructions.md)**: Rules and standards for AI agents working on this codebase.

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- SQLite (bundled with EF Core)

### Build & Run
```bash
dotnet restore
dotnet build
dotnet run
```
The API will be available at `http://localhost:5000` (or similar, check launch logs).
Swagger UI is available at the root URL.

## 🛠 Development

### Database Migrations
This project uses two DbContexts: `AuthDbContext` and `AppDbContext`.
See [ServerGuide.md](ServerGuide.md#12-migration--ef-core-operations) for migration commands.

### Git Hooks
To ensure documentation stays up-to-date, please install the pre-commit hook:

```bash
chmod +x scripts/install-hooks.sh
./scripts/install-hooks.sh
```

This hook will warn you if you modify source code (Controllers, Models, DTOs, etc.) without updating `ServerGuide.md`.
