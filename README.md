# Timetracker

Lightweight desktop time-tracking app with SQLite persistence, cached DB sets, and hotkey-driven Stop/StopStart flows.

## Features
- Start / Stop / Pause / Stop+Start operations
- Optional Stop prompt to collect description and metadata
- EF Core SQLite persistence with shared-PK EntryDescription
- Cached snapshot layer for fast UI reads and transactional writes
- Hotkeys: Ctrl+F1 = StopStart, Ctrl+F2 = Stop

## Prerequisites
- .NET SDK 7.0 or later
- (Optional) Visual Studio, Rider, or VS Code

## Quick start

1. Clone the repo
```bash
git clone <repo-url>
cd <repo-folder>
```

2. Restore and build
```bash
dotnet restore
dotnet build
```

3. Apply EF migrations (option A: CLI)
```bash
dotnet ef database update --project Timetracker.Infrastructure --startup-project Timetracker.UI.Wpf
```

4. Run the app
```bash
dotnet run --project Timetracker.UI.Wpf
```

5. Run tests
```bash
dotnet test
```