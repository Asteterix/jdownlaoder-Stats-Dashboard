# Steering: jdownloader Stats

## Session Rule
This steering file must be considered in every session. Read it before planning or changing code, and follow it for all work in this workspace.

## Purpose
WPF desktop application (.NET 10) that displays download statistics from a JDownloader instance, either locally or via the JDownloader cloud API.

## Project Structure
- `jdownloader Stats.csproj` - main WPF application
  - `Models/` - data models (e.g. `DownloadSnapshot`)
  - `Services/` - data sources, parsing, LAN discovery, settings, secret protection, formatting
  - `MainWindow.xaml(.cs)`, `SetupWindow.xaml(.cs)`, `App.xaml(.cs)` - UI
- `tests/JDownloaderStats.Tests/` - xUnit test project
- `vendor/My.Jdownloader.Api/` - vendored JDownloader API client. Treat as third-party code: avoid modifying it unless required, and keep changes minimal.
- `.github/workflows/release.yml` - release pipeline

## Architecture Conventions
- Data access goes through interfaces (e.g. `IDownloadStatsSource`) with concrete implementations (`LocalJDownloaderStatsSource`, `CloudJDownloaderStatsSource`).
- Keep UI logic in the window code-behind or view models; keep parsing, formatting and networking in `Services/`.
- Credentials must be protected via `SecretProtector`; never store secrets in plain text or commit them (`login.json` is local only).

## Coding Standards
- Target framework: .NET 10. Use modern C# features where they improve clarity (file-scoped namespaces, nullable reference types, `async`/`await`).
- Use async I/O for network calls; do not block the UI thread.
- Follow existing naming and formatting in the surrounding code.
- Add comments only where logic is non-obvious.
- Prefer existing libraries; do not add new packages without need.

## Testing
- Add or update xUnit tests in `tests/JDownloaderStats.Tests/` for changes to `Services/` and `Models/`.
- Run the full solution build and tests before finishing a change.
- All unit tests must pass before any check-in or push. Do not commit or push with failing tests.

## Build & Release
- Build: `dotnet build "jdownloader Stats.slnx"`
- Test: `dotnet test "jdownloader Stats.slnx"`
- Releases are produced by the GitHub Actions workflow in `.github/workflows/release.yml`. Do not manually edit files in `publish/`.

## Do Not
- Do not commit secrets, credentials or local settings.
- Do not modify generated or published artifacts (`publish/`, `bin/`, `obj/`).
- Do not change the vendored API client without a clear reason.
