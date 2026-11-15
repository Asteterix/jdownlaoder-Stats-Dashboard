# jDownloader Stats Dashboard

A small Windows WPF dashboard that shows live download statistics from a JDownloader instance connected to My.JDownloader. It displays current speed, active and queued downloads, overall progress, estimated time remaining, and a speed graph. The window can be kept on top of other windows, and the title bar hides while it is pinned.

## Features

- Connects to My.JDownloader with your account credentials
- Polls statistics every second
- Speed graph of the last 120 samples
- "Im Vordergrund" (always on top) option that hides the title bar; drag the window to move it
- Credentials are stored locally and protected with Windows DPAPI for the current user

## Requirements

- Windows
- .NET 10 SDK
- Visual Studio 2026 (or the `dotnet` CLI)
- A My.JDownloader account with a connected JDownloader instance

## Getting Started

```powershell
git clone --recurse-submodules https://github.com/<your-account>/jDownloaderStatsDashboard.git
cd jDownloaderStatsDashboard
dotnet build "jdownloader Stats.slnx"
dotnet test "jdownloader Stats.slnx"
dotnet run --project "jdownloader Stats.csproj"
```

If you cloned without `--recurse-submodules`, run `git submodule update --init --recursive`.

On first start, open **Setup** and enter your My.JDownloader e-mail and password. Then click **Verbinden** (Connect).

## Project Structure

| Path | Purpose |
| --- | --- |
| `Services/` | Settings, secret protection, My.JDownloader statistics source, speed history |
| `Models/` | Download snapshot model |
| `MainWindow.xaml(.cs)` | Dashboard window |
| `SetupWindow.xaml(.cs)` | Credentials dialog |
| `vendor/My.Jdownloader.Api` | Vendored My.JDownloader API client (Git submodule) |
| `tests/JDownloaderStats.Tests` | xUnit tests |

## Security

- Never commit `login.json` or other files with credentials. It is listed in `.gitignore`.
- Passwords are stored only as DPAPI-protected data in the local user profile.

## Licensing

This application is licensed under the GNU General Public License v3.0 because it includes My.Jdownloader.Api, which is GPL-3.0 licensed. See [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Use of AI

This software was implemented with the assistance of an AI coding assistant (GitHub Copilot, powered by AI models). All generated code was reviewed, built, and tested by the author.

## Credits

- [My.Jdownloader.Api](https://github.com/Cr1TiKa7/My.Jdownloader.Api) by Cr1TiKa7 (GPL-3.0), which is itself based on the JDownloader My.JDownloader API.
- [JDownloader](https://jdownloader.org/) is a separate project; this dashboard is not affiliated with it.
